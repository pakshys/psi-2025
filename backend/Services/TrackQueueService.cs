using backend.Database;
using backend.Extensions;
using backend.Models;
using backend.Dtos;
using Microsoft.EntityFrameworkCore;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Options;


namespace backend.Services;

public class TrackQueueService : ITrackQueueService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IYoutubeMetadataService _youtubeMetadataService;

    public TrackQueueService(ApplicationDbContext dbContext, IYoutubeMetadataService youtubeMetadataService)
    {
        _dbContext = dbContext;
        _youtubeMetadataService = youtubeMetadataService;
    }

    public async Task EnqueueAsync(int roomId, string trackId)
    {
        var room = await _dbContext.PartyRooms
            .Include(r => r.Tracks)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            throw new KeyNotFoundException("Party room not found.");

        var position = room.Tracks.Count;

        var item = new Track
        {
            TrackId = trackId,
            Position = position,
            PartyRoomId = roomId
        };

        _dbContext.Tracks.Add(item);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Track?> DequeueAsync(int roomId)
    {
        var firstItem = await _dbContext.Tracks
            .Where(p => p.PartyRoomId == roomId)
            .OrderBy(p => p.Position)
            .FirstOrDefaultAsync();

        if (firstItem == null)
            return null;

        _dbContext.Tracks.Remove(firstItem);

        var remainingItems = await _dbContext.Tracks
            .Where(p => p.PartyRoomId == roomId && p.Position > firstItem.Position)
            .ToListAsync();

        foreach (var item in remainingItems)
        {
            item.Position--;
        }

        await _dbContext.SaveChangesAsync();
        return firstItem;
    }

    public async Task<Track?> PeekAsync(int roomId)
    {
        return await _dbContext.Tracks
            .Where(p => p.PartyRoomId == roomId)
            .OrderBy(p => p.Position)
            .FirstOrDefaultAsync();
    }

    public async Task<List<TrackDto>> GetTrackQueueAsync(int roomId)
    {
        var tracks = await _dbContext.Tracks
            .Where(p => p.PartyRoomId == roomId)
            .OrderBy(p => p.Position)
            .ToListAsync();

        if (!tracks.Any())
        {
            // No videos in queue — return a placeholder
            return new List<TrackDto>
            {
                new TrackDto("placeholder", 0, "No video loaded", "")
            };
        }

        var trackDtos = new List<TrackDto>();

        foreach (var track in tracks)
        {
            trackDtos.Add(await _youtubeMetadataService.GetTrackDtoAsync(track));
        }

        return trackDtos;
    }

}
