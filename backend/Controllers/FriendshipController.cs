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
        public async Task<ActionResult<IEnumerable<FriendSummary>>> List()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var data = await _service.GetSummariesAsync(user.Id);
            return Ok(data);
        }

        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<FriendSummary>>> Pending()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var data = await _service.GetPendingSummariesAsync(user.Id);
            return Ok(data);
        }

        [HttpPost("add/{targetUserId}")]
        public async Task<IActionResult> AddFriend(string targetUserId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            try
            {
                var request = await _service.SendRequestAsync(user.Id, targetUserId);
                return Ok(new { message = "Friend request sent.", requestId = request.Id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost("accept/{id}")]
        public async Task<IActionResult> Accept(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            try
            {
                await _service.AcceptRequestAsync(id, user.Id);
                return Ok(new { message = "Friend request accepted." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("reject/{id}")]
        public async Task<IActionResult> Reject(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            try
            {
                await _service.RejectRequestAsync(id, user.Id);
                return Ok(new { message = "Friend request rejected." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}
