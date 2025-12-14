using backend.Models;

public interface IFriendshipService
{
    Task<List<Friendship>> GetFriendsAsync(string userId);
    Task<List<Friendship>> GetPendingAsync(string userId);
    Task<Friendship> SendRequestAsync(string requesterId, string addresseeId);
    Task AcceptRequestAsync(int friendshipId, string userId);
    Task RejectRequestAsync(int friendshipId, string userId);
    Task<List<FriendSummary>> GetSummariesAsync(string userId);
    Task<List<FriendSummary>> GetPendingSummariesAsync(string userId);

}