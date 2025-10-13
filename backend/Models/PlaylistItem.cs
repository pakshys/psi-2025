namespace backend.Models;

public class PlaylistItem
{
    public int Id { get; set; }
    public int PartyRoomId { get; set; }

    public string TrackId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }

    // Position in the playlist (lower = earlier)
    public int Position { get; set; }
}