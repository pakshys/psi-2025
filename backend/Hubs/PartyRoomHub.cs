using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs
{
    public class PartyRoomHub : Hub
    {
        
        private readonly TrackQueueService _trackQueueService;

        public PartyRoomHub(TrackQueueService trackQueueService)
        {
            _trackQueueService = trackQueueService;
        }

        // === Playback state tracking ===
        private record RoomPlaybackState(string VideoId, double CurrentTime, bool IsPlaying, DateTime LastUpdatedUtc);
        private static readonly Dictionary<string, RoomPlaybackState> _currentRoomStates = new();
        private static readonly Dictionary<string, Dictionary<string, string>> _votes = new();

        // === Active room connections ===
        private static readonly Dictionary<int, HashSet<string>> _roomConnections = new(); 

        // === Chat ===
        public async Task SendMessage(string roomId, string user, string message)
        {
            await Clients.Group(roomId).SendAsync("ReceiveMessage", user, message);
        }

        // === Join ===
        public async Task JoinRoom(int roomId)
        {
            string roomKey = roomId.ToString();
            await Groups.AddToGroupAsync(Context.ConnectionId, roomKey);

            // Track user in memory
            if (!_roomConnections.ContainsKey(roomId))
                _roomConnections[roomId] = new HashSet<string>();
            _roomConnections[roomId].Add(Context.ConnectionId);

            // Send current queue to the new user
            var queue = await _trackQueueService.GetTrackQueueAsync(roomId);
            await Clients.Caller.SendAsync("QueueUpdated", roomId, queue);

            // Sync video playback
            if (_currentRoomStates.TryGetValue(roomKey, out var state))
            {
                double effectiveTime = state.CurrentTime;
                if (state.IsPlaying)
                {
                    var elapsed = (DateTime.UtcNow - state.LastUpdatedUtc).TotalSeconds;
                    effectiveTime = Math.Max(0, state.CurrentTime + elapsed);
                }

                await Clients.Caller.SendAsync("LoadVideo", state.VideoId);
                await Clients.Caller.SendAsync("SeekTo", effectiveTime);

                if (state.IsPlaying)
                    await Clients.Caller.SendAsync("Play");
                else
                    await Clients.Caller.SendAsync("Pause");
            }

            // Broadcast updated member count
            int members = _roomConnections[roomId].Count;
            await Clients.Group(roomKey).SendAsync("MemberCountUpdated", members);
        }

        // Leave party room group
        public async Task LeaveRoom(int roomId)
        {
            string key = roomId.ToString();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, key);

            if (_roomConnections.TryGetValue(roomId, out var connections))
            {
                connections.Remove(Context.ConnectionId);
                int members = connections.Count;
                await Clients.Group(key).SendAsync("MemberCountUpdated", members);
            }

            await Clients.Group(key).SendAsync("UserLeft", roomId, Context.ConnectionId);
        }

        // Enqueue a track
        public async Task EnqueueTrack(int roomId, string trackId)
        {
            await _trackQueueService.EnqueueAsync(roomId, trackId);
            var queue = await _trackQueueService.GetTrackQueueAsync(roomId);

            // Update queue for everyone
            await Clients.Group(roomId.ToString()).SendAsync("QueueUpdated", roomId, queue);

            // Auto-load the new track for everyone
            var now = DateTime.UtcNow;
            _currentRoomStates[roomId.ToString()] = new RoomPlaybackState(trackId, 0.0, false, now);
            await Clients.Group(roomId.ToString()).SendAsync("LoadVideo", trackId);
        }

        // Dequeue a track
        public async Task<Track?> DequeueTrack(int roomId)
        {
            var dequeued = await _trackQueueService.DequeueAsync(roomId);
            var queue = await _trackQueueService.GetTrackQueueAsync(roomId);

            await Clients.Group(roomId.ToString()).SendAsync("QueueUpdated", roomId, queue);
            return dequeued;
        }

        // Peek at the next track
        public async Task<Track?> PeekNextTrack(int roomId)
        {
            return await _trackQueueService.PeekAsync(roomId);
        }

        // VOTING
        public async Task RequestVote(string roomId, string action)
        {
            if (!_votes.ContainsKey(roomId))
                _votes[roomId] = new Dictionary<string, string>();

            _votes[roomId].Clear(); // reset previous votes
            await Clients.Group(roomId).SendAsync("VoteRequested", action);
        }

        public async Task CastVote(string roomId, string userId, string action, bool agree)
        {
            if (!_votes.ContainsKey(roomId)) return;

            // Prevent duplicate votes
            if (_votes[roomId].ContainsKey(userId)) return;

            _votes[roomId][userId] = agree ? "yes" : "no";

            var yesVotes = _votes[roomId].Values.Count(v => v == "yes");
            var totalMembers = 1; // fallback

            if (int.TryParse(roomId, out var rid) && _roomConnections.ContainsKey(rid))
                totalMembers = _roomConnections[rid].Count;

            double participation = yesVotes / (double)Math.Max(1, totalMembers);

            if (participation >= 0.5)
            {
                if (action == "Play") await Play(roomId);
                if (action == "Pause") await Pause(roomId);

                await Clients.Group(roomId).SendAsync("VoteResult", action, true);
                _votes.Remove(roomId); // clear vote state after completion
            }
        }
        
        


        // UI commands
        public async Task LoadVideo(string roomId, string videoId)
        {
            var now = DateTime.UtcNow;
            _currentRoomStates[roomId] = new RoomPlaybackState(videoId, 0.0, false, now);
            await Clients.Group(roomId).SendAsync("LoadVideo", videoId);
        }

        // Play: if there is state, mark as playing and set LastUpdatedUtc
        public async Task Play(string roomId)
        {
            if (_currentRoomStates.TryGetValue(roomId, out var state))
            {
                // if previously paused we keep CurrentTime as-is; just mark playing and set LastUpdatedUtc
                _currentRoomStates[roomId] = state with { IsPlaying = true, LastUpdatedUtc = DateTime.UtcNow };
            }

            await Clients.Group(roomId).SendAsync("Play");
        }

        // Pause: update stored current time to whatever it currently is (so we don't drift)
        public async Task Pause(string roomId)
        {
            if (_currentRoomStates.TryGetValue(roomId, out var state))
            {
                // keep same current time, but mark not playing and record timestamp
                _currentRoomStates[roomId] = state with { IsPlaying = false, LastUpdatedUtc = DateTime.UtcNow };
            }

            await Clients.Group(roomId).SendAsync("Pause");
        }

        // UpdateTime: frontend periodically sends the latest time; store it along with timestamp
        public async Task UpdateTime(string roomId, double currentTime)
        {
            if (_currentRoomStates.TryGetValue(roomId, out var state))
            {
                _currentRoomStates[roomId] = state with { CurrentTime = currentTime, LastUpdatedUtc = DateTime.UtcNow };
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            foreach (var kv in _roomConnections)
            {
                if (kv.Value.Remove(Context.ConnectionId))
                {
                    int members = kv.Value.Count;
                    await Clients.Group(kv.Key.ToString()).SendAsync("MemberCountUpdated", members);
                    break;
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

    }
}
