using backend.Database;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class PartyRoomService
{
  private readonly ApplicationDbContext _context;

  public PartyRoomService(ApplicationDbContext context)
  {
    _context = context;
  }

  // Get all rooms
  public async Task<List<PartyRoom>> GetAllAsync()
  {
    return await _context.PartyRooms.ToListAsync();
  }

  // Get room by ID
  public async Task<PartyRoom?> GetByIdAsync(int id)
  {
    return await _context.PartyRooms.FindAsync(id);
  }

  // Create a new room
  public async Task<PartyRoom> CreateAsync(string name, int capacity = 10, bool isPrivate = false)
  {
    if (string.IsNullOrWhiteSpace(name))
      throw new ArgumentException("Party room name cannot be empty.");

    if (capacity <= 0)
      throw new ArgumentException("Party room capacity must be greater than zero.");

    var partyRoom = new PartyRoom
    {
      Name = name,
      Capacity = capacity,
      IsPrivate = isPrivate
    };

    _context.PartyRooms.Add(partyRoom);
    await _context.SaveChangesAsync();
    return partyRoom;
  }

  // Join a room
  // NOTE: this service does not manage which user is a member (no userId parameter).
  // It only enforces capacity based on the Members list length. The Hub should be
  // responsible for adding/removing user ids to `PartyRoom.Members` and persisting that.
  public async Task JoinAsync(int id)
  {
    var room = await GetByIdAsync(id);
    if (room == null)
      throw new KeyNotFoundException("Party room not found.");

    var currentCount = room.Members?.Count ?? 0;
    if (currentCount >= room.Capacity)
      throw new InvalidOperationException("Party room is full.");

    // Do not modify GuestsCount here — treat Members list as the source of truth.
    // The Hub (or caller) should add the user id into room.Members and persist that.
  }

  // Leave a room
  // Similar to JoinAsync: we validate but do not mutate GuestsCount. The caller/hub
  // should remove the user id from the Members list and persist.
  public async Task LeaveAsync(int id)
  {
    var room = await GetByIdAsync(id);
    if (room == null)
      throw new KeyNotFoundException("Party room not found.");

    var currentCount = room.Members?.Count ?? 0;
    if (currentCount <= 0)
      throw new InvalidOperationException("Party room is already empty.");

    // Do not modify GuestsCount here.
  }

  // Update a room
  public async Task UpdateAsync(PartyRoom updatedRoom)
  {
    var existingRoom = await GetByIdAsync(updatedRoom.Id);
    if (existingRoom == null)
      throw new KeyNotFoundException("Party room not found.");

    if (string.IsNullOrWhiteSpace(updatedRoom.Name))
      throw new ArgumentException("Party room name cannot be empty.");

    if (updatedRoom.Capacity <= 0)
      throw new ArgumentException("Party room capacity must be greater than zero.");

    var currentCount = existingRoom.Members?.Count ?? 0;
    if (updatedRoom.Capacity < currentCount)
      throw new InvalidOperationException("New capacity cannot be less than current guests count.");

    existingRoom.Name = updatedRoom.Name;
    existingRoom.Capacity = updatedRoom.Capacity;
    await _context.SaveChangesAsync();
  }

  // Delete a room
  public async Task DeleteAsync(int id)
  {
    var room = await GetByIdAsync(id);
    if (room == null)
      throw new KeyNotFoundException("Party room not found.");

    _context.PartyRooms.Remove(room);
    await _context.SaveChangesAsync();
  }
}