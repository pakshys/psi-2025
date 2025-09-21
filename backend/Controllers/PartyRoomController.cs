using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
public class PartyRoomController : ControllerBase
{
    public PartyRoomController()
    {
    }

    // GET all action
    [HttpGet]
    public ActionResult<List<PartyRoom>> GetAll() =>
        PartyRoomService.GetAll();

    // GET by id action
    [HttpGet("{id}")]
    public ActionResult<PartyRoom> Get(int id)
    {
        var partyRoom = PartyRoomService.Get(id);

        if (partyRoom is null)
            return NotFound();

        return partyRoom;
    }

    // POST action
    [HttpPost]
    public IActionResult Create(PartyRoom partyRoom)
    {
        PartyRoomService.Add(partyRoom);
        return CreatedAtAction(nameof(Get), new { id = partyRoom.Id }, partyRoom);
    }

    // PUT action
    [HttpPut("{id}")]
    public IActionResult Update(int id, PartyRoom partyRoom)
    {
        if (id != partyRoom.Id)
            return BadRequest();

        var existingPartyRoom = PartyRoomService.Get(id);
        if (existingPartyRoom is null)
            return NotFound();

        PartyRoomService.Update(partyRoom);
        return NoContent();
    }

    // DELETE action
    [HttpDelete("{id}")]
    public IActionResult Detele(int id)
    {
        var partyRoom = PartyRoomService.Get(id);

        if (partyRoom is null)
            return NotFound();

        PartyRoomService.Delete(id);
        return NoContent();
    }
}