using backend.Models;

namespace backend.Services;

public class PartyRoomService
{
    private static List<PartyRoom> PartyRooms { get; }
    private static int nextId = 1;

    static PartyRoomService()
    {
        PartyRooms = new List<PartyRoom>
        { 
            // Default party room for demonstration purposes
            new PartyRoom { Id = nextId++, Name = "Default room", Capacity = 10, IsPrivate = false }
        };
    }

    public static List<PartyRoom> GetAll() => PartyRooms;

    public static PartyRoom? Get(int id) => PartyRooms.FirstOrDefault(p => p.Id == id);

    public static void Add(PartyRoom partyRoom)
    {
        if (string.IsNullOrWhiteSpace(partyRoom.Name))
            throw new ArgumentException("Name cannot be empty.");

        if (partyRoom.Capacity <= 0)
            throw new ArgumentException("Capacity must be greater than zero.");

        partyRoom.Id = nextId++;
        PartyRooms.Add(partyRoom);
    }

    public static void Delete(int id)
    {
        var partyRoom = Get(id);
        if (partyRoom is null)
            return;

        PartyRooms.Remove(partyRoom);
    }

    public static void Update(PartyRoom partyRoom)
    {
        var index = PartyRooms.FindIndex(p => p.Id == partyRoom.Id);
        if (index == -1)
            return;

        PartyRooms[index] = partyRoom;
    }


}