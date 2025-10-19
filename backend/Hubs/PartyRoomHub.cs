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
    private readonly TrackQueueService _trackQueueService;

    public PartyRoomHub(TrackQueueService trackQueueService, ApplicationDbContext dbContext)
    {
      _trackQueueService = trackQueueService;
      _dbContext = dbContext;
    }

    // === Playback state tracking ===
    private record RoomPlaybackState(string VideoId, double CurrentTime, bool IsPlaying, DateTime LastUpdatedUtc);
    private static readonly Dictionary<string, RoomPlaybackState> _currentRoomStates = new();
    private static readonly Dictionary<string, Dictionary<string, string>> _votes = new();
    private static readonly Dictionary<string, RoomVote> _activeVotes = new();

    // === Active room connections ===
    private static readonly Dictionary<int, HashSet<string>> _roomUsers = new();

    // === Chat ===
    public async Task SendMessage(string roomId, string user, string message)
    {
      await Clients.Group(roomId).SendAsync("ReceiveMessage", user, message);
    }

    // === Join Room ===
    public async Task JoinRoom(int roomId)
    {
      string roomKey = roomId.ToString();
      var userId = Context.UserIdentifier!;

      // Remove user from any other rooms first
      foreach (var kv in _roomUsers.ToList())
      {
        if (kv.Value.Remove(userId))
        {
          await Groups.RemoveFromGroupAsync(Context.ConnectionId, kv.Key.ToString());
          await Clients.Group(kv.Key.ToString()).SendAsync("MemberListUpdated", kv.Value.ToList());

          // Update DB for that room
          var oldRoom = await _dbContext.PartyRooms.FindAsync(kv.Key);
          if (oldRoom != null)
          {
            oldRoom.GuestsCount = kv.Value.Count;
            oldRoom.Members = new List<string>(kv.Value); // sync members
            await _dbContext.SaveChangesAsync();
          }
        }
      }

      if (!_roomUsers.ContainsKey(roomId))
        _roomUsers[roomId] = new HashSet<string>();

      _roomUsers[roomId].Add(userId);

      await Groups.AddToGroupAsync(Context.ConnectionId, roomKey);

      // Update DB for this room
      var room = await _dbContext.PartyRooms.FindAsync(roomId);
      if (room != null)
      {
        room.GuestsCount = _roomUsers[roomId].Count;
        room.Members = new List<string>(_roomUsers[roomId]); // sync members
        await _dbContext.SaveChangesAsync();
      }

      // Broadcast updated members
      await Clients.Group(roomKey).SendAsync("MemberListUpdated", _roomUsers[roomId].ToList());

      // Send current queue and playback state to caller
      var queue = await _trackQueueService.GetTrackQueueAsync(roomId);
      await Clients.Caller.SendAsync("QueueUpdated", roomId, queue);

      if (_currentRoomStates.TryGetValue(roomKey, out var state))
      {
        double effectiveTime = state.CurrentTime;
        if (state.IsPlaying)
        {
          effectiveTime += (DateTime.UtcNow - state.LastUpdatedUtc).TotalSeconds;
        }

        await Clients.Caller.SendAsync("SyncTime", new { videoId = state.VideoId, time = effectiveTime, isPlaying = state.IsPlaying });
      }
    }

    // === Leave Room ===
    public async Task LeaveRoom(int roomId)
    {
      string roomKey = roomId.ToString();
      var userId = Context.UserIdentifier!;

      await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomKey);

      if (_roomUsers.TryGetValue(roomId, out var users))
      {
        users.Remove(userId);

        // Update DB
        var room = await _dbContext.PartyRooms.FindAsync(roomId);
        if (room != null)
        {
          room.GuestsCount = users.Count;
          room.Members = new List<string>(users); // sync members
          await _dbContext.SaveChangesAsync();
        }

        await Clients.Group(roomKey).SendAsync("MemberListUpdated", users.ToList());
      }

      await Clients.Group(roomKey).SendAsync("UserLeft", roomId, userId);
    }

    // === Queue Management ===
    public async Task EnqueueTrack(int roomId, string trackId)
    {
      await _trackQueueService.EnqueueAsync(roomId, trackId);
      var queue = await _trackQueueService.GetTrackQueueAsync(roomId);

      await Clients.Group(roomId.ToString()).SendAsync("QueueUpdated", roomId, queue);

      string roomKey = roomId.ToString();
      bool shouldAutoPlay = false;

      if (!_currentRoomStates.ContainsKey(roomKey) || !_currentRoomStates[roomKey].IsPlaying)
      {
        var now = DateTime.UtcNow;
        var firstTrack = queue.FirstOrDefault();
        if (firstTrack != null && (!_currentRoomStates.ContainsKey(roomKey) || _currentRoomStates[roomKey].VideoId != firstTrack.TrackId))
        {
          _currentRoomStates[roomKey] = new RoomPlaybackState(firstTrack.TrackId, 0.0, false, now);
          shouldAutoPlay = true;
        }
      }

      if (shouldAutoPlay)
      {
        _currentRoomStates[roomKey] = _currentRoomStates[roomKey] with { IsPlaying = true, LastUpdatedUtc = DateTime.UtcNow };
        await Clients.Group(roomKey).SendAsync("LoadVideo", _currentRoomStates[roomKey].VideoId);
        await Clients.Group(roomKey).SendAsync("Play");
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
        _currentRoomStates[roomId.ToString()] = new RoomPlaybackState(nextTrack.TrackId, 0.0, true, now);
        await Clients.Group(roomId.ToString()).SendAsync("LoadVideo", nextTrack.TrackId);
        await Clients.Group(roomId.ToString()).SendAsync("Play");
      }
    }

    public async Task<Track?> DequeueTrack(int roomId)
    {
      var dequeued = await _trackQueueService.DequeueAsync(roomId);
      var queue = await _trackQueueService.GetTrackQueueAsync(roomId);

      await Clients.Group(roomId.ToString()).SendAsync("QueueUpdated", roomId, queue);
      return dequeued;
    }

    public async Task<Track?> PeekNextTrack(int roomId)
    {
      return await _trackQueueService.PeekAsync(roomId);
    }

    // === Voting ===
    public async Task RequestVote(string roomId, string action)
    {
      if (_activeVotes.ContainsKey(roomId))
        _activeVotes.Remove(roomId);

      _activeVotes[roomId] = new RoomVote
      {
        Action = action,
        StartTime = DateTime.UtcNow
      };

      await Clients.Group(roomId).SendAsync("VoteRequested", action);
    }

    public async Task CastVote(string roomId, string userId, bool agree)
    {
      if (!_activeVotes.ContainsKey(roomId)) return;

      var roomVote = _activeVotes[roomId];
      if (roomVote.Votes.ContainsKey(userId)) return;

      roomVote.Votes[userId] = agree;

      int yesVotes = roomVote.Votes.Values.Count(v => v);
      int totalMembers = 1;
      if (int.TryParse(roomId, out int rid) && _roomUsers.ContainsKey(rid))
        totalMembers = _roomUsers[rid].Count;

      double participation = yesVotes / (double)Math.Max(1, totalMembers);

      if (participation >= 0.5)
      {
        await ApplyVoteAction(roomId, roomVote.Action);
        await Clients.Group(roomId).SendAsync("VoteResult", roomVote.Action, true);
        _activeVotes.Remove(roomId);
      }
      else
      {
        await Clients.Group(roomId).SendAsync("VoteProgress", roomId, roomVote.Votes.Count, totalMembers);
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
          await Play(roomId);
          break;
        case "Pause":
          await Pause(roomId);
          break;
      }
    }

    private class RoomVote
    {
      public string Action { get; set; } = "";
      public Dictionary<string, bool> Votes { get; set; } = new();
      public DateTime StartTime { get; set; } = DateTime.UtcNow;
    }

    private async Task CheckVoteTimeouts()
    {
      var now = DateTime.UtcNow;
      var expiredVotes = _activeVotes.Where(kv => (now - kv.Value.StartTime).TotalSeconds > 30).ToList();

      foreach (var kv in expiredVotes)
      {
        await Clients.Group(kv.Key).SendAsync("VoteResult", kv.Value.Action, false);
        _activeVotes.Remove(kv.Key);
      }
    }

    // === Playback Controls ===
    public async Task LoadVideo(string roomId, string videoId)
    {
      var now = DateTime.UtcNow;
      _currentRoomStates[roomId] = new RoomPlaybackState(videoId, 0.0, false, now);
      await Clients.Group(roomId).SendAsync("LoadVideo", videoId);
    }

    public async Task Play(string roomId, double? currentTime = null)
    {
      if (_currentRoomStates.TryGetValue(roomId, out var state))
      {
        var newTime = currentTime ?? state.CurrentTime;
        _currentRoomStates[roomId] = state with { CurrentTime = newTime, IsPlaying = true, LastUpdatedUtc = DateTime.UtcNow };
        var updated = _currentRoomStates[roomId];
        await Clients.Group(roomId).SendAsync("SyncTime", new { videoId = updated.VideoId, time = updated.CurrentTime, isPlaying = true });
      }
      else
      {
        await Clients.Group(roomId).SendAsync("Play");
      }
    }

    public async Task Pause(string roomId, double? currentTime = null)
    {
      if (_currentRoomStates.TryGetValue(roomId, out var state))
      {
        var newTime = currentTime ?? state.CurrentTime;
        _currentRoomStates[roomId] = state with { CurrentTime = newTime, IsPlaying = false, LastUpdatedUtc = DateTime.UtcNow };
        var updated = _currentRoomStates[roomId];
        await Clients.Group(roomId).SendAsync("SyncTime", new { videoId = updated.VideoId, time = updated.CurrentTime, isPlaying = false });
      }
      else
      {
        await Clients.Group(roomId).SendAsync("Pause");
      }
    }

    public async Task UpdateTime(string roomId, double currentTime)
    {
      if (_currentRoomStates.TryGetValue(roomId, out var state))
      {
        _currentRoomStates[roomId] = state with { CurrentTime = currentTime, LastUpdatedUtc = DateTime.UtcNow };
        var updated = _currentRoomStates[roomId];
        await Clients.Group(roomId).SendAsync("SyncTime", new { videoId = updated.VideoId, time = updated.CurrentTime, isPlaying = updated.IsPlaying });
      }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
      var userId = Context.UserIdentifier!;
      foreach (var kv in _roomUsers)
      {
        if (kv.Value.Remove(userId))
        {
          await Clients.Group(kv.Key.ToString()).SendAsync("MemberListUpdated", kv.Value.ToList());
          break;
        }
      }

      await base.OnDisconnectedAsync(exception);
    }
  }
}
