namespace Dtos;

public record TrackDto(string TrackId, int Position, string Title = "", string Creator = "");