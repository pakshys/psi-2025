using backend.Exceptions;
using backend.Database;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class PartyRoomService : IPartyRoomService
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<PartyRoomService> _logger;

  public PartyRoomService(ApplicationDbContext context, ILogger<PartyRoomService> logger)
  {
    _context = context;
    _logger = logger;
  }

  // Get all rooms
  public async Task<List<PartyRoom>> GetAllAsync()
  {
    _logger.LogInformation("Fetching all party rooms");
    return await _context.PartyRooms.ToListAsync();
  }

  // Get room by ID - throws NotFoundException
  public async Task<PartyRoom> GetByIdAsync(int id)
  {
    _logger.LogInformation("Fetching party room with ID: {RoomId}", id);
    
    var room = await _context.PartyRooms.FindAsync(id);
    
    if (room == null)
    {
      _logger.LogWarning("Party room with ID {RoomId} not found", id);
      throw new NotFoundException($"Party room with ID {id} not found.");
    }
    
    return room;
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
    
    _logger.LogInformation("Created party room: {RoomName} (ID: {RoomId})", name, partyRoom.Id);
    return partyRoom;
  }

  // Join a room
  public async Task JoinAsync(int id)
  {
    var room = await GetByIdAsync(id); // Will throw NotFoundException if not found

    var currentCount = room.Members?.Count ?? 0;
    if (currentCount >= room.Capacity)
      throw new InvalidOperationException("Party room is full.");
    
    _logger.LogInformation("User joined party room: {RoomId}", id);
  }

  // Leave a room
  public async Task LeaveAsync(int id)
  {
    var room = await GetByIdAsync(id); // Will throw NotFoundException if not found

    var currentCount = room.Members?.Count ?? 0;
    if (currentCount <= 0)
      throw new InvalidOperationException("Party room is already empty.");
    
    _logger.LogInformation("User left party room: {RoomId}", id);
  }

  // Update a room
  public async Task UpdateAsync(PartyRoom updatedRoom)
  {
    var existingRoom = await GetByIdAsync(updatedRoom.Id); // Will throw NotFoundException if not found

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
    
    _logger.LogInformation("Updated party room: {RoomId}", updatedRoom.Id);
  }

  // Delete a room
  public async Task DeleteAsync(int id)
  {
    var room = await GetByIdAsync(id); // Will throw NotFoundException if not found

    _context.PartyRooms.Remove(room);
    await _context.SaveChangesAsync();
    
    _logger.LogInformation("Deleted party room: {RoomId}", id);
  }
}