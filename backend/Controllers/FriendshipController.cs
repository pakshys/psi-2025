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
    public class FriendshipController : ControllerBase
    {
        private readonly FriendshipService _service;
        private readonly UserManager<User> _userManager;

        public FriendshipController(FriendshipService service, UserManager<User> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        [HttpGet("list")]
        public async Task<ActionResult<List<Friendship>>> GetMyFriends()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            return await _service.GetFriendsAsync(user.Id);
        }

        [HttpGet("pending")]
        public async Task<ActionResult<List<Friendship>>> GetPending()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            return await _service.GetPendingAsync(user.Id);
        }

        [HttpPost("add/{targetUserId}")]
        public async Task<IActionResult> AddFriend(string targetUserId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            await _service.SendRequestAsync(user.Id, targetUserId);
            return Ok(new { message = "Friend request sent." });
        }

        [HttpPost("accept/{id}")]
        public async Task<IActionResult> Accept(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            await _service.AcceptRequestAsync(id, user.Id);
            return Ok(new { message = "Friend request accepted." });
        }

        [HttpDelete("reject/{id}")]
        public async Task<IActionResult> Reject(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            await _service.RejectRequestAsync(id, user.Id);
            return Ok(new { message = "Friend request rejected." });
        }
    }
}
