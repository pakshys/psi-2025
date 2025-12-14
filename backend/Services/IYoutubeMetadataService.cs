using backend.Dtos;
using backend.Models;

public interface IYoutubeMetadataService
{
    Task<TrackDto> GetTrackDtoAsync(Track track);
}