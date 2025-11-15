namespace backend.Models;

public record RoomPlaybackState(
    string VideoId,
    double CurrentTime,
    bool IsPlaying,
    DateTime LastUpdatedUtc
);
