using backend.Models;
using backend.Dtos;

namespace backend.Extensions;

public static class MappingExtensions
{
    public static TrackDto ToDto(this Track t) =>
        new TrackDto(t.TrackId, t.Position);
}