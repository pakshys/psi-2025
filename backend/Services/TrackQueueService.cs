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
    private readonly string _apiKey;

    public TrackQueueService(ApplicationDbContext dbContext, IOptions<YouTubeSettings> settings)
    {
        _dbContext = dbContext;
        _apiKey = settings.Value.ApiKey;
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

        var trackDtos = new List<TrackDto>();

        // Initialize YouTube API
        var youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = _apiKey,
            ApplicationName = "psi-2025"
        });

        if (!tracks.Any())
        {
            // No videos in queue — return a placeholder
            trackDtos.Add(new TrackDto(
                TrackId: "placeholder",
                Position: 0,
                Title: "No video loaded",
                Creator: ""
            ));
            return trackDtos;
        }

        foreach (var track in tracks)
        {
            try
            {
                // Clean the TrackId: remove playlist/index params
                var cleanTrackId = track.TrackId.Split('&')[0];

                var videoRequest = youtubeService.Videos.List("snippet");
                videoRequest.Id = cleanTrackId;
                var response = await videoRequest.ExecuteAsync();

                var video = response.Items.FirstOrDefault();
                if (video != null)
                {
                    trackDtos.Add(new TrackDto(
                        TrackId: track.TrackId,
                        Position: track.Position,
                        Title: video.Snippet.Title,
                        Creator: video.Snippet.ChannelTitle
                    ));
                }
                else
                {
                    // Video not found — show safe fallback
                    trackDtos.Add(new TrackDto(
                        TrackId: track.TrackId,
                        Position: track.Position,
                        Title: "Video unavailable",
                        Creator: ""
                    ));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching video {track.TrackId}: {ex.Message}");
                trackDtos.Add(new TrackDto(
                    TrackId: track.TrackId,
                    Position: track.Position,
                    Title: "Video unavailable",
                    Creator: ""
                ));
            }
        }

        return trackDtos;
    }

}
