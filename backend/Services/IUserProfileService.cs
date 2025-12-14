using backend.Models;

public interface IUserProfileService
{
    Task<string> UploadProfilePictureAsync(IFormFile file);
    FileStream GetProfilePictureStream(string fileName);
    Task<UserProfile?> GetByUserIdAsync(string userId);
    Task<List<UserProfile>> GetAllAsync();
    Task<UserProfile> CreateOrUpdateAsync(string userId, string displayName, string? bio, string? pictureUrl);

}