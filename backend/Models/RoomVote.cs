namespace backend.Models;

public class RoomVote
{
    public string Action { get; set; } = "";
    public Dictionary<string, bool> Votes { get; set; } = new();
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
}