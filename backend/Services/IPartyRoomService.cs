using backend.Models;

public interface IPartyRoomService
{
    Task<List<PartyRoom>> GetAllAsync();
    Task<PartyRoom> GetByIdAsync(int id);
    Task<PartyRoom> CreateAsync(string name, int capacity, bool isPrivate, string? password);
    Task JoinAsync(int id, string? password);
    Task LeaveAsync(int id);
    Task UpdateAsync(PartyRoom updatedRoom);
    Task DeleteAsync(int id);

}