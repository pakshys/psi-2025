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
    public async Task<PartyRoom> CreateAsync(PartyRoom partyRoom)
    {
        if (string.IsNullOrWhiteSpace(partyRoom.Name))
            throw new ArgumentException("Party room name cannot be empty.");

        if (partyRoom.Capacity <= 0)
            throw new ArgumentException("Party room capacity must be greater than zero.");

        _context.PartyRooms.Add(partyRoom);
        await _context.SaveChangesAsync();
        return partyRoom;
    }

    // Join a room
    public async Task JoinAsync(int id)
    {
        var room = await GetByIdAsync(id);
        if (room == null)
            throw new ArgumentException("Party room not found.");

        if (room.GuestsCount >= room.Capacity)
            throw new InvalidOperationException("Party room is full.");

        room.GuestsCount++;
        await _context.SaveChangesAsync();
    }

    // Leave a room
    public async Task LeaveAsync(int id)
    {
        var room = await GetByIdAsync(id);
        if (room == null)
            throw new ArgumentException("Party room not found.");

        if (room.GuestsCount <= 0)
            throw new InvalidOperationException("Party room is already empty.");

        room.GuestsCount--;
        await _context.SaveChangesAsync();
    }

    // Update a room
    public async Task UpdateAsync(PartyRoom updatedRoom)
    {
        var existingRoom = await GetByIdAsync(updatedRoom.Id);
        if (existingRoom == null)
            throw new ArgumentException("Party room not found.");

        if (string.IsNullOrWhiteSpace(updatedRoom.Name))
            throw new ArgumentException("Party room name cannot be empty.");

        if (updatedRoom.Capacity <= 0)
            throw new ArgumentException("Party room capacity must be greater than zero.");

        if (updatedRoom.Capacity < existingRoom.GuestsCount)
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
            throw new ArgumentException("Party room not found.");

        _context.PartyRooms.Remove(room);
        await _context.SaveChangesAsync();
    }
}