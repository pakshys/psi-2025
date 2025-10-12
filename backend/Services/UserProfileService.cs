using backend.Database;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class UserProfileService
    {
        private readonly ApplicationDbContext _context;

        public UserProfileService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfile?> GetByUserIdAsync(string userId)
        {
            return await _context.UserProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<List<UserProfile>> GetAllAsync()
        {
            return await _context.UserProfiles
                .Include(p => p.User)
                .ToListAsync();
        }

        public async Task<UserProfile> CreateOrUpdateAsync(string userId, string displayName, string? bio, string? pictureUrl)
        {
            var profile = await GetByUserIdAsync(userId);
            if (profile == null)
            {
                profile = new UserProfile
                {
                    UserId = userId,
                    DisplayName = displayName,
                    Bio = bio,
                    ProfilePictureUrl = pictureUrl
                };
                _context.UserProfiles.Add(profile);
            }
            else
            {
                profile.DisplayName = displayName;
                profile.Bio = bio;
                profile.ProfilePictureUrl = pictureUrl;
            }

            await _context.SaveChangesAsync();
            return profile;
        }
    }
}
