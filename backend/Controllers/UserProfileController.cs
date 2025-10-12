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
    }
}
