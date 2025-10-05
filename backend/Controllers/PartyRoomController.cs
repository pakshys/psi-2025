using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
public class PartyRoomController : ControllerBase
{
    private readonly PartyRoomService _service;
    public PartyRoomController(PartyRoomService service)
    {
        _service = service;
    }

    // GET all action
    [HttpGet]
    public async Task<ActionResult<List<PartyRoom>>> GetAll()
    {
        return await _service.GetAllAsync();
    }

    // GET by id action
    [HttpGet("{id}")]
    public async Task<ActionResult<PartyRoom>> Get(int id)
    {
        var partyRoom = await _service.GetByIdAsync(id);

        if (partyRoom is null)
            return NotFound();

        return partyRoom;
    }

    // POST action
    [HttpPost]
    public async Task<IActionResult> Create(PartyRoom partyRoom)
    {
        try
        {
            var createdRoom = await _service.CreateAsync(partyRoom);
            return CreatedAtAction(nameof(Get), new { id = createdRoom.Id }, createdRoom);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST join action
    [HttpPost("{id}/join")]
    public async Task<IActionResult> Join(int id)
    {
        try
        {
            await _service.JoinAsync(id);

            var partyRoom = await _service.GetByIdAsync(id);
            return Ok(partyRoom);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST leave action
    [HttpPost("{id}/leave")]
    public async Task<IActionResult> Leave(int id)
    {
        try
        {
            await _service.LeaveAsync(id);

            var partyRoom = await _service.GetByIdAsync(id);
            return Ok(partyRoom);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // PUT action
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, PartyRoom partyRoom)
    {
        if (id != partyRoom.Id)
            return BadRequest(new { error = "ID in URL does not match ID in body." });

        try
        {
            await _service.UpdateAsync(partyRoom);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        
    }

    // DELETE action
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}