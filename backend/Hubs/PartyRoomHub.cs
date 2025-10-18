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

            // Send the current queue to the user
            var queue = await _trackQueueService.GetTrackQueueAsync(roomId);
            await Clients.Caller.SendAsync("QueueUpdated", roomId, queue);
            await Clients.Group(roomKey).SendAsync("UserJoined", roomId);

            // Sync YouTube state
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
        }

        // Leave party room group
        public async Task LeaveRoom(int roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
            await Clients.Group(roomId.ToString()).SendAsync("UserLeft", roomId, Context.ConnectionId);
        }

        // Enqueue a track
        public async Task EnqueueTrack(int roomId, string trackId)
        {
            await _trackQueueService.EnqueueAsync(roomId, trackId);
            var queue = await _trackQueueService.GetTrackQueueAsync(roomId);

            await Clients.Group(roomId.ToString()).SendAsync("QueueUpdated", roomId, queue);
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

            _votes[roomId][userId] = agree ? "yes" : "no";

            var totalVotes = _votes[roomId].Count;
            var yesVotes = _votes[roomId].Values.Count(v => v == "yes");

            // Assume you track usersCount in DB or memory (fallback to 2 for demo)
            double participation = yesVotes / (double)Math.Max(1, totalVotes);

            if (participation >= 0.5)
            {
                if (action == "Play") await Play(roomId);
                if (action == "Pause") await Pause(roomId);
                await Clients.Group(roomId).SendAsync("VoteResult", action, true);
            }
            else
            {
                await Clients.Group(roomId).SendAsync("VoteResult", action, false);
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

    }
}
