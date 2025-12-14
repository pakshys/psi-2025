public record CreatePartyRoomDto(
    string Name,
    int Capacity,
    bool IsPrivate,
    string? Password
);