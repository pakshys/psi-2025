using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs
{
    public class PartyRoomHub : Hub
    {
        
        private record RoomPlaybackState(string VideoId, double CurrentTime, bool IsPlaying, DateTime LastUpdatedUtc);

        // Store current playback state for syncing new members
        private static readonly Dictionary<string, RoomPlaybackState> _currentRoomStates = new();

        // Chat System
        public async Task SendMessage(string roomId, string user, string message)
        {
            await Clients.Group(roomId).SendAsync("ReceiveMessage", user, message);
        }

        // Join party room group
        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // Notify everyone in the room that a new user joined
            await Clients.Group(roomId).SendAsync("UserJoined", roomId);

            // If there's an active video, sync it for the joining user
            if (_currentRoomStates.TryGetValue(roomId, out var state))
            {
                // Compute elapsed time if playing
                double effectiveTime = state.CurrentTime;
                if (state.IsPlaying)
                {
                    var elapsed = (DateTime.UtcNow - state.LastUpdatedUtc).TotalSeconds;
                    effectiveTime = state.CurrentTime + elapsed;
                    // optional: clamp to a minimum of 0
                    if (effectiveTime < 0) effectiveTime = 0;
                }

                // Send video id, seek to computed time, then set play/pause
                await Clients.Caller.SendAsync("LoadVideo", state.VideoId);
                await Clients.Caller.SendAsync("SeekTo", effectiveTime);

                if (state.IsPlaying)
                    await Clients.Caller.SendAsync("Play");
                else
                    await Clients.Caller.SendAsync("Pause");
            }
        }

        // Leave party room group
        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("UserLeft", roomId, Context.ConnectionId);
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
