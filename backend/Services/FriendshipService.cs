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

        public async Task<List<Friendship>> GetFriendsAsync(string userId)
        {
            return await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f =>
                    (f.RequesterId == userId || f.AddresseeId == userId) &&
                    f.Status == FriendshipStatus.Accepted)
                .ToListAsync();
        }

        public async Task<List<Friendship>> GetPendingAsync(string userId)
        {
            return await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
                .ToListAsync();
        }

        public async Task<Friendship> SendRequestAsync(string requesterId, string addresseeId)
        {
            if (requesterId == addresseeId)
                throw new ArgumentException("Cannot add yourself as a friend.");

            var existing = await _context.Friendships.FirstOrDefaultAsync(f =>
                (f.RequesterId == requesterId && f.AddresseeId == addresseeId) ||
                (f.RequesterId == addresseeId && f.AddresseeId == requesterId));

            if (existing != null)
                throw new InvalidOperationException("Friend request already exists or users are already friends.");

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

            if (friendship.AddresseeId != userId)
                throw new UnauthorizedAccessException("You are not allowed to accept this request.");

            friendship.Status = FriendshipStatus.Accepted;
            await _context.SaveChangesAsync();
        }

        public async Task RejectRequestAsync(int id, string userId)
        {
            var friendship = await _context.Friendships.FindAsync(id)
                ?? throw new KeyNotFoundException("Friend request not found.");

            if (friendship.AddresseeId != userId)
                throw new UnauthorizedAccessException("You are not allowed to reject this request.");

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
        }

        public async Task<List<FriendSummary>> GetSummariesAsync(string userId)
        {
            return await _context.Friendships
                .Include(f => f.Requester).Include(f => f.Addressee)
                .Where(f => f.RequesterId == userId || f.AddresseeId == userId)
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

        public async Task<List<FriendSummary>> GetPendingSummariesAsync(string userId)
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
    }
}
