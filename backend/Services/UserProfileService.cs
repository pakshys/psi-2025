using backend.Database;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly ApplicationDbContext _context;

        private readonly string _uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

        public UserProfileService(ApplicationDbContext context)
        {
            _context = context;
            if (!Directory.Exists(_uploadDir))
              Directory.CreateDirectory(_uploadDir);
        }

        
        // Upload profile picture
        public async Task<string> UploadProfilePictureAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(_uploadDir, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }

        // Get profile picture stream
        public FileStream GetProfilePictureStream(string fileName)
        {
            var filePath = Path.Combine(_uploadDir, fileName);
            if (!File.Exists(filePath))
                throw new FileNotFoundException();

            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
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
