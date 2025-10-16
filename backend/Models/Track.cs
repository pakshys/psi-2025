namespace backend.Models;

public class Track : IComparable<Track> 
{
    // Primary key
    public int Id { get; set; }

    public string TrackId { get; set; } = string.Empty;
    // Position in the playlist (lower = earlier)
    public int Position { get; set; }

    // Foreign key to PartyRoom
    public int PartyRoomId { get; set; }
    public PartyRoom? PartyRoom { get; set; }

    public int CompareTo(Track? other)
    {
        if (other is null) return 1;
        return Position.CompareTo(other.Position);
    }
}