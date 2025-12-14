using backend.Exceptions;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
public class PartyRoomController : ControllerBase
{
    private readonly PartyRoomService _partyRoomService;

    public PartyRoomController(PartyRoomService service)
    {
        _partyRoomService = service;
    }

    // GET all action
    [HttpGet]
    public async Task<ActionResult<List<PartyRoom>>> GetAll()
    {
        return await _partyRoomService.GetAllAsync();
    }

    // GET by id action
    [HttpGet("{id}")]
    public async Task<ActionResult<PartyRoom>> Get(int id)
    {
        // Service will throw NotFoundException if not found
        // Middleware will catch it and return proper 404
        var partyRoom = await _partyRoomService.GetByIdAsync(id);
        return partyRoom;
    }

    // POST create action
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePartyRoomDto dto)
    {
        // Let middleware handle ArgumentException
        var createdRoom = await _partyRoomService.CreateAsync(
            dto.Name,
            dto.Capacity,
            dto.IsPrivate,
            dto.Password
        );
        
        // Count the creator as the first member
        createdRoom.GuestsCount = 1;
        await _partyRoomService.UpdateAsync(createdRoom);
        
        return CreatedAtAction(nameof(Get), new { id = createdRoom.Id }, createdRoom);
    }

    // POST join action
    [HttpPost("{id}/join")]
    public async Task<IActionResult> Join(int id, [FromBody] JoinPartyRoomDto dto)
    {
        // Let middleware handle exceptions
        await _partyRoomService.JoinAsync(id, dto.Password);
        var partyRoom = await _partyRoomService.GetByIdAsync(id);
        return Ok(partyRoom);
    }

    // POST leave action
    [HttpPost("{id}/leave")]
    public async Task<IActionResult> Leave(int id)
    {
        // Let middleware handle exceptions
        await _partyRoomService.LeaveAsync(id);
        var partyRoom = await _partyRoomService.GetByIdAsync(id);
        return Ok(partyRoom);
    }

    // PUT update action
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, PartyRoom partyRoom)
    {
        if (id != partyRoom.Id)
            return BadRequest(new { error = "ID in URL does not match ID in body." });
        
        // Let middleware handle exceptions
        await _partyRoomService.UpdateAsync(partyRoom);
        return NoContent();
    }

    // DELETE action
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Let middleware handle NotFoundException
        await _partyRoomService.DeleteAsync(id);
        return NoContent();
    }
}