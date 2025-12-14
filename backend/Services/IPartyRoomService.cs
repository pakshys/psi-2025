using backend.Models;

public interface IPartyRoomService
{
    Task<List<PartyRoom>> GetAllAsync();
    Task<PartyRoom> GetByIdAsync(int id);
    Task<PartyRoom> CreateAsync(string name, int capacity = 10, bool isPrivate = false);
    Task JoinAsync(int id);
    Task LeaveAsync(int id);
    Task UpdateAsync(PartyRoom updatedRoom);
    Task DeleteAsync(int id);

}