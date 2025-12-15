using backend.Dtos;
using backend.Models;

namespace backend.Services;

public interface IYouTubeMetadataService
{
    Task<TrackDto> GetTrackDtoAsync(Track track);
}