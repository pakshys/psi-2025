using backend.Models;

public interface IFriendshipService
{
    Task<Friendship> SendRequestAsync(string requesterId, string addresseeId);
    Task AcceptRequestAsync(int id, string userId);
    Task RejectRequestAsync(int id, string userId);
    Task<List<FriendSummary>> GetAcceptedSummariesAsync(string userId);
    Task<List<FriendSummary>> GetIncomingPendingSummariesAsync(string userId);
    Task<List<FriendSummary>> GetOutgoingPendingSummariesAsync(string userId);
    Task CancelOutgoingRequestAsync(int friendshipId, string userId);
    Task RemoveFriendAsync(int friendshipId, string userId);
    

}