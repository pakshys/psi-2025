using backend.Models;

public interface IRoomStateService
{
    bool TryGetPlayback(string roomId, out RoomPlaybackState state);
    void SetPlayback(string roomId, RoomPlaybackState state);
    bool HasPlayback(string roomId);
    RoomPlaybackState? GetPlayback(string roomId);
    void EnsureRoomExists(int roomId);
    void AddUserToRoom(int roomId, string userId);
    bool RemoveUserFromRoom(int roomId, string userId);
    List<string> GetUsersInRoom(int roomId);
    List<int> RemoveUserFromAllRooms(string userId);
    bool RoomExists(int roomId);

    IReadOnlyDictionary<int, DateTime> GetEmptyRooms();
    void DeleteRoom(int roomId);
}