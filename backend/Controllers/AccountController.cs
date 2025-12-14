using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models;
using backend.Database;
using System.Threading.Tasks;

namespace backend.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class AccountController : ControllerBase
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IUserProfileService _profileService;

        public AccountController(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            IUserProfileService profileService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _profileService = profileService;
        }


        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Create an empty user profile right after successful registration
                await _profileService.CreateOrUpdateAsync(user.Id, model.UserName, null, null);

                // Login the user after registration
                await _signInManager.SignInAsync(user, isPersistent: false);

                return Ok(new { Message = "Registration successful and profile created." });
            }

            return BadRequest(new
            {
                Message = "Registration failed",
                Errors = result.Errors
            });
        }

        [HttpPost("Login")]
		public async Task<IActionResult> Login([FromBody] LoginViewModel model)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

            User? user = await _userManager.FindByNameAsync(model.Login)
                 ?? await _userManager.FindByEmailAsync(model.Login);

            var result = await _signInManager.PasswordSignInAsync(
				user.UserName,
				model.Password,
				model.RememberMe,
				lockoutOnFailure: false
			);

			if (result.Succeeded)
				return Ok(new { Message = "Login successful" });

			return Unauthorized(new { Message = "Invalid login attempt" });
		}

		[HttpPost("Logout")]
		public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
			return Ok(new { Message = "Logged out successfully" });
		}

		[HttpGet("Me")]
		public async Task<IActionResult> GetCurrentUser()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return NotFound(new { Message = "User not logged in" });

			return Ok(new
			{
				user.Id,
				user.UserName,
				user.Email
			});
		}
	}
}