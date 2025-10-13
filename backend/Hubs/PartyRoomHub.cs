using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs
{
    public class PartyRoomHub : Hub
    {
        //Chat System
        public async Task SendMessage(string roomId, string user, string message)
        {
            await Clients.Group(roomId).SendAsync("ReceiveMessage", user, message);
        }

        //Member count refresh
        public async Task SendMessage(string roomId, string user, string message)
        {
            await Clients.Group(roomId).SendAsync("ReceiveMessage", user, message);
        }

        // Join party room group
        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("UserJoined", roomId, Context.ConnectionId);
        }

        // Leave party room group
        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("UserLeft", roomId, Context.ConnectionId);
        }

        //UI commands
        public async Task LoadVideo(string roomId, string videoId)
        {
            await Clients.Group(roomId).SendAsync("LoadVideo", videoId);
        }

        public async Task Play(string roomId)
        {
            await Clients.Group(roomId).SendAsync("Play");
        }

        public async Task Pause(string roomId)
        {
            await Clients.Group(roomId).SendAsync("Pause");
        }
    }
}