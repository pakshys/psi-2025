using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class UserProfileController : ControllerBase
    {
        private readonly UserProfileService _service;
        private readonly UserManager<User> _userManager;

        public UserProfileController(UserProfileService service, UserManager<User> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<UserProfile>> GetProfile(string userId)
        {
            var profile = await _service.GetByUserIdAsync(userId);
            if (profile == null)
                return NotFound();
            return profile;
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserProfile>> GetMyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var profile = await _service.GetByUserIdAsync(user.Id);
            if (profile == null)
                return NotFound();

            return profile;
        }

        [HttpPost("update")]
        public async Task<ActionResult<UserProfile>> UpdateProfile([FromBody] UserProfile model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var updated = await _service.CreateOrUpdateAsync(user.Id, model.DisplayName, model.Bio, model.ProfilePictureUrl);
            return Ok(updated);
        }

        [HttpPost("upload-picture")]
        public async Task<ActionResult<UserProfile>> UploadProfilePicture([FromForm] IFormFile profile)
        {
          var user = await _userManager.GetUserAsync(User);
          if (user == null)
            return Unauthorized();

          if (profile == null || profile.Length == 0)
            return BadRequest("No file uploaded");

          // Save file to disk using a GUID filename
          var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
          if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

          var fileName = $"{Guid.NewGuid()}_{profile.FileName}";
          var filePath = Path.Combine(uploadsFolder, fileName);

          await using (var stream = new FileStream(filePath, FileMode.Create))
          {
            await profile.CopyToAsync(stream);
          }

          // Update UserProfile with new picture filename
          var updatedProfile = await _service.CreateOrUpdateAsync(
            user.Id,
            displayName: (await _service.GetByUserIdAsync(user.Id))?.DisplayName ?? user.UserName!,
            bio: (await _service.GetByUserIdAsync(user.Id))?.Bio,
            pictureUrl: fileName
          );

          return Ok(updatedProfile);
        }

        [AllowAnonymous]
        [HttpGet("picture/{fileName}")]
        public IActionResult GetProfilePicture(string fileName)
        {
          var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
          var filePath = Path.Combine(uploadsFolder, fileName);

          if (!System.IO.File.Exists(filePath))
            return NotFound();

          var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
          return File(stream, "image/jpeg"); // Adjust MIME type if needed
        }
  }
}
