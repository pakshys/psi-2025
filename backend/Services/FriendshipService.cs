using backend.Database;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class FriendshipService
    {
        private readonly ApplicationDbContext _context;

        public FriendshipService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Friendship> SendRequestAsync(string requesterId, string addresseeId)
        {
            if (requesterId == addresseeId)
                throw new ArgumentException("Cannot add yourself as a friend.");

            var existing = await _context.Friendships.FirstOrDefaultAsync(f =>
                (f.RequesterId == requesterId && f.AddresseeId == addresseeId) ||
                (f.RequesterId == addresseeId && f.AddresseeId == requesterId));

            if (existing != null)
            {
                if (existing.Status == FriendshipStatus.Accepted)
                    throw new InvalidOperationException("Users are already friends.");

                if (existing.Status == FriendshipStatus.Pending)
                    throw new InvalidOperationException("Friend request already exists.");

                throw new InvalidOperationException("Friend request cannot be created.");
            }

            var friendship = new Friendship
            {
                RequesterId = requesterId,
                AddresseeId = addresseeId,
                Status = FriendshipStatus.Pending
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();
            return friendship;
        }

        public async Task AcceptRequestAsync(int id, string userId)
        {
            var friendship = await _context.Friendships.FindAsync(id)
                ?? throw new KeyNotFoundException("Friend request not found.");

            if (friendship.Status != FriendshipStatus.Pending)
                throw new InvalidOperationException("This request is not pending.");

            if (friendship.AddresseeId != userId)
                throw new UnauthorizedAccessException("You are not allowed to accept this request.");

            friendship.Status = FriendshipStatus.Accepted;
            await _context.SaveChangesAsync();
        }

        public async Task RejectRequestAsync(int id, string userId)
        {
            var friendship = await _context.Friendships.FindAsync(id)
                ?? throw new KeyNotFoundException("Friend request not found.");

            if (friendship.Status != FriendshipStatus.Pending)
                throw new InvalidOperationException("This request is not pending.");

            if (friendship.AddresseeId != userId)
                throw new UnauthorizedAccessException("You are not allowed to reject this request.");

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
        }

        public async Task<List<FriendSummary>> GetAcceptedSummariesAsync(string userId)
        {
            return await _context.Friendships
                .Include(f => f.Requester).Include(f => f.Addressee)
                .Where(f =>
                    (f.RequesterId == userId || f.AddresseeId == userId) &&
                    f.Status == FriendshipStatus.Accepted)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FriendSummary(
                    f.Id,
                    f.RequesterId == userId ? f.AddresseeId : f.RequesterId,
                    f.RequesterId == userId
                        ? (f.Addressee.UserName ?? f.AddresseeId)
                        : (f.Requester.UserName ?? f.RequesterId),
                    f.Status,
                    f.CreatedAt
                ))
                .ToListAsync();
        }

        public async Task<List<FriendSummary>> GetIncomingPendingSummariesAsync(string userId)
        {
            return await _context.Friendships
                .Include(f => f.Requester)
                .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FriendSummary(
                    f.Id,
                    f.RequesterId,
                    f.Requester.UserName ?? f.RequesterId,
                    f.Status,
                    f.CreatedAt
                ))
                .ToListAsync();
        }

        public async Task<List<FriendSummary>> GetOutgoingPendingSummariesAsync(string userId)
        {
            return await _context.Friendships
                .Include(f => f.Addressee)
                .Where(f => f.RequesterId == userId && f.Status == FriendshipStatus.Pending)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FriendSummary(
                    f.Id,
                    f.AddresseeId,
                    f.Addressee.UserName ?? f.AddresseeId,
                    f.Status,
                    f.CreatedAt
                ))
                .ToListAsync();
        }

        public async Task CancelOutgoingRequestAsync(int friendshipId, string userId)
        {
            var friendship = await _context.Friendships.FindAsync(friendshipId)
                ?? throw new KeyNotFoundException("Friend request not found.");

            if (friendship.RequesterId != userId)
                throw new UnauthorizedAccessException("You are not allowed to cancel this request.");

            if (friendship.Status != FriendshipStatus.Pending)
                throw new InvalidOperationException("Only pending requests can be cancelled.");

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveFriendAsync(int friendshipId, string userId)
        {
            var friendship = await _context.Friendships.FindAsync(friendshipId)
                ?? throw new KeyNotFoundException("Friendship not found.");

            if (friendship.RequesterId != userId && friendship.AddresseeId != userId)
                throw new UnauthorizedAccessException("You are not allowed to remove this friend.");

            if (friendship.Status != FriendshipStatus.Accepted)
                throw new InvalidOperationException("Only accepted friends can be removed.");

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
        }
    }
}
