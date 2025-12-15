using backend.Models;
using backend.Services;
using backend.Database;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace backend.Hubs
{
  [Authorize]
  public class PartyRoomHub : Hub
  {
    private readonly ApplicationDbContext _dbContext;
    private readonly ITrackQueueService _trackQueueService;
    private readonly IRoomStateService _roomStateService;
    private readonly IVoteService _voteService;

    public PartyRoomHub(
      ITrackQueueService trackQueueService,
      ApplicationDbContext dbContext,
      IRoomStateService roomStateService,
      IVoteService voteService)
    {
      _trackQueueService = trackQueueService;
      _dbContext = dbContext;
      _roomStateService = roomStateService;
      _voteService = voteService;
    }

    // === Chat ===
    public async Task SendMessage(string roomId, string user, string message)
    {
      await Clients.Group(roomId).SendAsync("ReceiveMessage", user, message);
    }

    // === Join Room ===
    public async Task JoinRoom(int roomId)
    {
      string roomKey = roomId.ToString();
      var userId = Context.UserIdentifier;

      if (string.IsNullOrEmpty(userId))
      {
        await Clients.Caller.SendAsync("Error", "UserIdentifier missing");
        return;
      }

      // Remove from all other rooms
      var oldRooms = _roomStateService.RemoveUserFromAllRooms(userId);

      foreach (var oldRoom in oldRooms)
      {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, oldRoom.ToString());

        var oldUserList = _roomStateService.GetUsersInRoom(oldRoom);
        await Clients.Group(oldRoom.ToString()).SendAsync("MemberListUpdated", oldUserList);

        var dbRoom = await _dbContext.PartyRooms.FindAsync(oldRoom);
        if (dbRoom != null)
        {
          dbRoom.Members = oldUserList;
          dbRoom.GuestsCount = oldUserList.Count;
          await _dbContext.SaveChangesAsync();
        }
      }

      // Add to new room
      _roomStateService.AddUserToRoom(roomId, userId);
      await Groups.AddToGroupAsync(Context.ConnectionId, roomKey);

      // Update DB
      var room = await _dbContext.PartyRooms.FindAsync(roomId);
      if (room != null)
      {
        var members = _roomStateService.GetUsersInRoom(roomId);
        room.Members = members;
        room.GuestsCount = members.Count;
        await _dbContext.SaveChangesAsync();
      }

      // Broadcast members
      await Clients.Group(roomKey).SendAsync("MemberListUpdated", _roomStateService.GetUsersInRoom(roomId));

      // Send queue
      var queue = await _trackQueueService.GetTrackQueueAsync(roomId);
      await Clients.Caller.SendAsync("QueueUpdated", roomId, queue);

      // Send playback state
      if (_roomStateService.TryGetPlayback(roomKey, out var state))
      {
        double effectiveTime = state.CurrentTime;
        if (state.IsPlaying)
        {
          effectiveTime += (DateTime.UtcNow - state.LastUpdatedUtc).TotalSeconds;
          if (effectiveTime < 0) effectiveTime = 0;
        }

        await Clients.Caller.SendAsync("SyncTime", new
        {
          videoId = state.VideoId,
          time = effectiveTime,
          isPlaying = state.IsPlaying
        });
      }
    }

    // === Leave Room ===
    public async Task LeaveRoom(int roomId)
    {
      string roomKey = roomId.ToString();
      var userId = Context.UserIdentifier;

      if (string.IsNullOrEmpty(userId))
      {
        await Clients.Caller.SendAsync("Error", "UserIdentifier missing");
        return;
      }

      await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomKey);

      if (_roomStateService.RemoveUserFromRoom(roomId, userId))
      {
        var members = _roomStateService.GetUsersInRoom(roomId);

        var room = await _dbContext.PartyRooms.FindAsync(roomId);
        if (room != null)
        {
          room.Members = members;
          room.GuestsCount = members.Count;
          await _dbContext.SaveChangesAsync();
        }

        await Clients.Group(roomKey).SendAsync("MemberListUpdated", members);
      }

      await Clients.Group(roomKey).SendAsync("UserLeft", roomId, userId);
    }

    // === Queue Management ===
    public async Task EnqueueTrack(int roomId, string trackId)
    {
      string roomKey = roomId.ToString();
      var now = DateTime.UtcNow;

      var playback = _roomStateService.HasPlayback(roomKey)
        ? _roomStateService.GetPlayback(roomKey)
        : null;

      bool isPlaceholder = playback == null || playback.VideoId == "PLACEHOLDER"; // Placeholder ID

      if (isPlaceholder)
      {
        // Replace placeholder and start playing immediately
        var state = new RoomPlaybackState(trackId, 0, true, now);
        _roomStateService.SetPlayback(roomKey, state);

        // Notify clients (include caller because server initiates playback change)
        await Clients.Group(roomKey).SendAsync("LoadVideo", trackId);
        await Clients.Group(roomKey).SendAsync("Play");

        // Queue remains empty
        var queue = await _trackQueueService.GetTrackQueueAsync(roomId);
        await Clients.Group(roomId.ToString()).SendAsync("QueueUpdated", roomId, queue);
      }
      else
      {
        // Normal enqueue
        await _trackQueueService.EnqueueAsync(roomId, trackId);
        var queue = await _trackQueueService.GetTrackQueueAsync(roomId);
        await Clients.Group(roomId.ToString()).SendAsync("QueueUpdated", roomId, queue);

        // Auto-play if nothing is currently playing
        if (!playback.IsPlaying)
        {
          var first = queue.FirstOrDefault();
          if (first != null)
          {
            var state = new RoomPlaybackState(first.TrackId, 0, true, now);
            _roomStateService.SetPlayback(roomKey, state);

            await Clients.Group(roomKey).SendAsync("LoadVideo", state.VideoId);
            await Clients.Group(roomKey).SendAsync("Play");
          }
        }
      }
    }

    public async Task SkipTrack(int roomId)
    {
      var nextTrack = await _trackQueueService.DequeueAsync(roomId);
      var queue = await _trackQueueService.GetTrackQueueAsync(roomId);

      await Clients.Group(roomId.ToString()).SendAsync("QueueUpdated", roomId, queue);

      if (nextTrack != null)
      {
        var now = DateTime.UtcNow;
        var state = new RoomPlaybackState(nextTrack.TrackId, 0, true, now);
        _roomStateService.SetPlayback(roomId.ToString(), state);

        await Clients.Group(roomId.ToString()).SendAsync("LoadVideo", nextTrack.TrackId);
        await Clients.Group(roomId.ToString()).SendAsync("Play");
      }
    }

    // === Voting ===
    public async Task RequestVote(string roomId, string action)
    {
      _voteService.StartVote(roomId, action);

      await Clients.Group(roomId).SendAsync("VoteRequested", action);
    }

    public async Task CastVote(string roomId, string userId, bool agree)
    {
      var totalMembers = 1;

      if (int.TryParse(roomId, out int rid))
        totalMembers = _roomStateService.GetUsersInRoom(rid).Count;

      if (!_voteService.TryCastVote(roomId, userId, agree,
          out int yesVotes, out int totalVotes))
      {
        return;
      }

      double participation = yesVotes / (double)Math.Max(1, totalMembers);

      if (participation >= 0.5)
      {
        await ApplyVoteAction(roomId, _voteService.GetVoteAction(roomId)!);
        await Clients.Group(roomId).SendAsync("VoteResult", _voteService.GetVoteAction(roomId), true);

        _voteService.RemoveVote(roomId);
      }
      else
      {
        await Clients.Group(roomId).SendAsync("VoteProgress", roomId, totalVotes, totalMembers);
      }
    }

    private async Task ApplyVoteAction(string roomId, string action)
    {
      switch (action)
      {
        case "Skip":
          if (int.TryParse(roomId, out int rid)) await SkipTrack(rid);
          break;
        case "Play":
          await BroadcastPlay(roomId);
          break;
        case "Pause":
          await BroadcastPause(roomId);
          break;
      }
    }

    private async Task BroadcastPlay(string roomId, double? currentTime = null)
    {
      if (_roomStateService.TryGetPlayback(roomId, out var state))
      {
        var updated = state with
        {
          CurrentTime = currentTime ?? state.CurrentTime,
          IsPlaying = true,
          LastUpdatedUtc = DateTime.UtcNow
        };

        _roomStateService.SetPlayback(roomId, updated);

        // Send to entire group so the vote-initiator also receives the update
        await Clients.Group(roomId).SendAsync("SyncTime", new
        {
          videoId = updated.VideoId,
          time = updated.CurrentTime,
          isPlaying = true
        });
      }
      else
      {
        await Clients.Group(roomId).SendAsync("Play");
      }
    }

    private async Task BroadcastPause(string roomId, double? currentTime = null)
    {
      if (_roomStateService.TryGetPlayback(roomId, out var state))
      {
        var updated = state with
        {
          CurrentTime = currentTime ?? state.CurrentTime,
          IsPlaying = false,
          LastUpdatedUtc = DateTime.UtcNow
        };

        _roomStateService.SetPlayback(roomId, updated);

        // Send to entire group so the vote-initiator also receives the update
        await Clients.Group(roomId).SendAsync("SyncTime", new
        {
          videoId = updated.VideoId,
          time = updated.CurrentTime,
          isPlaying = false
        });
      }
      else
      {
        await Clients.Group(roomId).SendAsync("Pause");
      }
    }

    // === Playback Controls ===
    public async Task LoadVideo(string roomId, string videoId)
    {
      var state = new RoomPlaybackState(videoId, 0, false, DateTime.UtcNow);
      _roomStateService.SetPlayback(roomId, state);

      await Clients.Group(roomId).SendAsync("LoadVideo", videoId);
    }

    public async Task Play(string roomId, double? currentTime = null)
    {
      if (_roomStateService.TryGetPlayback(roomId, out var state))
      {
        var updated = state with
        {
          CurrentTime = currentTime ?? state.CurrentTime,
          IsPlaying = true,
          LastUpdatedUtc = DateTime.UtcNow
        };

        _roomStateService.SetPlayback(roomId, updated);

        await Clients.OthersInGroup(roomId).SendAsync("SyncTime", new
        {
          videoId = updated.VideoId,
          time = updated.CurrentTime,
          isPlaying = true
        });
      }
      else
      {
        await Clients.OthersInGroup(roomId).SendAsync("Play");
      }
    }

    public async Task Pause(string roomId, double? currentTime = null)
    {
      if (_roomStateService.TryGetPlayback(roomId, out var state))
      {
        var updated = state with
        {
          CurrentTime = currentTime ?? state.CurrentTime,
          IsPlaying = false,
          LastUpdatedUtc = DateTime.UtcNow
        };

        _roomStateService.SetPlayback(roomId, updated);

        // Broadcast to others only
        await Clients.OthersInGroup(roomId).SendAsync("SyncTime", new
        {
          videoId = updated.VideoId,
          time = updated.CurrentTime,
          isPlaying = false
        });
      }
      else
      {
        await Clients.OthersInGroup(roomId).SendAsync("Pause");
      }
    }

    public async Task UpdateTime(string roomId, double currentTime)
    {
      if (_roomStateService.TryGetPlayback(roomId, out var state))
      {
        if (Math.Abs(currentTime - state.CurrentTime) < 2) return;

        var updated = state with
        {
          CurrentTime = currentTime,
          LastUpdatedUtc = DateTime.UtcNow
        };

        _roomStateService.SetPlayback(roomId, updated);

        await Clients.OthersInGroup(roomId).SendAsync("SyncTime", new
        {
          videoId = updated.VideoId,
          time = updated.CurrentTime,
          isPlaying = updated.IsPlaying
        });
      }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
      var userId = Context.UserIdentifier;

      if (string.IsNullOrEmpty(userId))
      {
        await base.OnDisconnectedAsync(exception);
        return;
      }

      // Remove user from all rooms
      var rooms = _roomStateService.RemoveUserFromAllRooms(userId);

      foreach (var roomId in rooms)
      {
        var members = _roomStateService.GetUsersInRoom(roomId);

        await Clients.Group(roomId.ToString()).SendAsync("MembersListUpdated", members);

        var room = await _dbContext.PartyRooms.FindAsync(roomId);
        if (room != null)
        {
          room.Members = members;
          room.GuestsCount = members.Count;
          await _dbContext.SaveChangesAsync();
        }
      }

      await base.OnDisconnectedAsync(exception);
    }
  }
}
