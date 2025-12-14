using System.Text.Json.Serialization;

namespace backend.Models;

public class PartyRoom
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Capacity { get; set; }
    public bool IsPrivate { get; set; }

    [JsonIgnore]
    public string? PasswordHash { get; set; }

    public List<string> Members { get; set; } = new();

    public int GuestsCount { get; set; } = 0;

    public List<Track> Tracks { get; set; } = new();

}