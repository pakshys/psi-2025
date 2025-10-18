using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs
{
    public class PartyRoomHub : Hub
    {
        public readonly TrackQueueService _trackQueueService;

        public PartyRoomHub(TrackQueueService trackQueueService)
        {
            _trackQueueService = trackQueueService;
        }

        // Join party room group
        public async Task JoinRoom(int roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
            var queue = await _trackQueueService.GetTrackQueueAsync(roomId);
            await Clients.Group(roomId.ToString()).SendAsync("UserJoined", roomId, Context.ConnectionId);
            await Clients.Caller.SendAsync("QueueUpdated", roomId, queue);
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
    }
}