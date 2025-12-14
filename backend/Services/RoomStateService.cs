using backend.Models;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Concurrent;

namespace backend.Services;

public class RoomStateService : IRoomStateService
{

    // Playback state for room
    private readonly ConcurrentDictionary<string, RoomPlaybackState> _currentRoomStates = new();

    // Active room users
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, byte>> _roomUsers = new();

    // Dictionary to store when a room became empty
    private readonly ConcurrentDictionary<int, DateTime> _emptyRoomTimestamps = new();


    // === Playback state ===
    public bool TryGetPlayback(string roomId, out RoomPlaybackState state)
    {
        return _currentRoomStates.TryGetValue(roomId, out state);
    }

    public void SetPlayback(string roomId, RoomPlaybackState state)
    {
        _currentRoomStates[roomId] = state;
    }

    public bool HasPlayback(string roomId)
    {
        return _currentRoomStates.ContainsKey(roomId);
    }

    public RoomPlaybackState? GetPlayback(string roomId)
    {
        if (_currentRoomStates.TryGetValue(roomId, out var state))
            return state;
        
        return null;
    }

    // === Room users ===
    public void EnsureRoomExists(int roomId)
    {
        _roomUsers.GetOrAdd(roomId, _ => new ConcurrentDictionary<string, byte>());
    }

    public void AddUserToRoom(int roomId, string userId)
    {
        EnsureRoomExists(roomId);
        _roomUsers[roomId].TryAdd(userId, 0);

        _emptyRoomTimestamps.TryRemove(roomId, out _);
    }

    public bool RemoveUserFromRoom(int roomId, string userId)
    {
        if (!_roomUsers.TryGetValue(roomId, out var users))
            return false;

        var removed = users.TryRemove(userId, out _);

        if (removed && users.IsEmpty)
        {
            // Mark the room as empty with the current time
            _emptyRoomTimestamps[roomId] = DateTime.UtcNow;
        }

        return removed;
    }

    public List<string> GetUsersInRoom(int roomId)
    {
        if (_roomUsers.TryGetValue(roomId, out var users))
            return users.Keys.ToList();

        return new List<string>();
    }

    public List<int> RemoveUserFromAllRooms(string userId)
    {
        var removedRooms = new List<int>();

        foreach (var kv in _roomUsers.ToList())
        {
            var roomId = kv.Key;
            var users = kv.Value;

            if (users.TryRemove(userId, out _))
                removedRooms.Add(roomId);
        }

        return removedRooms;
    }
    
    public bool RoomExists(int roomId)
    {
        return _roomUsers.ContainsKey(roomId);
    }

    public IReadOnlyDictionary<int, DateTime> GetEmptyRooms()
    {
      return _emptyRoomTimestamps;
    }

    public void DeleteRoom(int roomId)
    {
      _roomUsers.TryRemove(roomId, out _);
      _currentRoomStates.TryRemove(roomId.ToString(), out _);
      _emptyRoomTimestamps.TryRemove(roomId, out _);
    }
}