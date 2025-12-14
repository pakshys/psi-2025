using backend.Models;
using backend.Dtos;

public interface ITrackQueueService
{
    Task EnqueueAsync(int roomId, string trackId);
    Task<Track?> DequeueAsync(int roomId);
    Task<Track?> PeekAsync(int roomId);
    Task<List<TrackDto>> GetTrackQueueAsync(int roomId);

}