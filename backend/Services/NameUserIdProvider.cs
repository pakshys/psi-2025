using Microsoft.AspNetCore.SignalR;

namespace backend.Services
{
    public class NameUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User?.Identity?.Name ?? connection.ConnectionId;
        }
    }
}