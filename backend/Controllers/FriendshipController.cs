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
        private readonly IFriendshipService _service;
        private readonly UserManager<User> _userManager;

        public FriendshipController(IFriendshipService service, UserManager<User> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        [HttpGet("list")]
        public async Task<ActionResult<IEnumerable<FriendSummary>>> List()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var data = await _service.GetAcceptedSummariesAsync(user.Id);
            return Ok(data);
        }

        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<FriendSummary>>> Pending()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var data = await _service.GetIncomingPendingSummariesAsync(user.Id);
            return Ok(data);
        }

        [HttpGet("outgoing")]
        public async Task<ActionResult<IEnumerable<FriendSummary>>> Outgoing()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var data = await _service.GetOutgoingPendingSummariesAsync(user.Id);
            return Ok(data);
        }

        [HttpPost("add/by-username/{username}")]
        public async Task<IActionResult> AddByUsername(string username)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { error = "Username is required." });

            var target = await _userManager.FindByNameAsync(username);
            if (target == null)
                return NotFound(new { error = "User not found." });

            try
            {
                var request = await _service.SendRequestAsync(user.Id, target.Id);
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
        }

        [HttpPost("accept/{id}")]
        public async Task<IActionResult> Accept(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

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

        [HttpDelete("cancel/{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            try
            {
                await _service.CancelOutgoingRequestAsync(id, user.Id);
                return Ok(new { message = "Friend request cancelled." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
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
            if (user == null) return Unauthorized();

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

        [HttpDelete("remove/{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            try
            {
                await _service.RemoveFriendAsync(id, user.Id);
                return Ok(new { message = "Friend removed." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}
