namespace backend.Models;

public class PartyRoom
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Capacity { get; set; }
    public bool IsPrivate { get; set; }
    public int GuestsCount { get; set; } = 0;
}