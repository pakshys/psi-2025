using backend.Hubs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
public class PartyRoomController : ControllerBase
{
    private readonly PartyRoomService _partyRoomService; // CAHNGE NAMING TO BE MORE DETAILED
    private readonly IHubContext<PartyRoomHub> _hubContext;

    public PartyRoomController(PartyRoomService service, IHubContext<PartyRoomHub> hubContext)
    {
        _partyRoomService = service;
        _hubContext = hubContext;
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
        var partyRoom = await _partyRoomService.GetByIdAsync(id);

        if (partyRoom is null)
            return NotFound();

        return partyRoom;
    }

    // POST create action
    [HttpPost]
    public async Task<IActionResult> Create([FromQuery] string name,
                                            [FromQuery] int capacity = 10,
                                            [FromQuery] bool isPrivate = false)
    {
        try
        {
            var createdRoom = await _partyRoomService.CreateAsync(name, capacity, isPrivate);

        // Count the creator as the first member
        createdRoom.GuestsCount = 1;
        await _partyRoomService.UpdateAsync(createdRoom);

        // Notify all clients about the created room and current state
        await _hubContext.Clients.All.SendAsync("PartyRoomCreated", createdRoom);
        await _hubContext.Clients.All.SendAsync("PartyRoomUpdated", createdRoom);

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
            await _partyRoomService.JoinAsync(id);
            var partyRoom = await _partyRoomService.GetByIdAsync(id);

            // Notify clients in the room about the new user joining
            await _hubContext.Clients.Group(id.ToString())
                .SendAsync("UserJoined", id, partyRoom!.GuestsCount);

            // Notify all clients about the updated party room
            await _hubContext.Clients.All.SendAsync("PartyRoomUpdated", partyRoom);

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
            await _partyRoomService.LeaveAsync(id);
            var partyRoom = await _partyRoomService.GetByIdAsync(id);

            // Notify clients in the room about the user leaving
            await _hubContext.Clients.Group(id.ToString())
                .SendAsync("UserLeft", id, partyRoom!.GuestsCount);

            // Notify all clients about the updated party room
            await _hubContext.Clients.All.SendAsync("PartyRoomUpdated", partyRoom);


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

    // PUT update action
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, PartyRoom partyRoom)
    {
        if (id != partyRoom.Id)
            return BadRequest(new { error = "ID in URL does not match ID in body." });

        try
        {
            await _partyRoomService.UpdateAsync(partyRoom);

            // Notify all clients about the updated party room
            await _hubContext.Clients.All.SendAsync("PartyRoomUpdated", partyRoom);

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
            await _partyRoomService.DeleteAsync(id);

            // Notify all clients about the deleted party room
            await _hubContext.Clients.All.SendAsync("PartyRoomDeleted", id);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}