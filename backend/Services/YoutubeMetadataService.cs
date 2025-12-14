using backend.Dtos;
using backend.Models;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Options;

public class YoutubeMetadataService : IYoutubeMetadataService
{
    private readonly YouTubeService _youtubeService;

    public YoutubeMetadataService(IOptions<YouTubeSettings> options)
    {
        // Initialize YouTube API
        _youtubeService = new YouTubeService(new BaseClientService.Initializer
        {
            ApiKey = options.Value.ApiKey,
            ApplicationName = "psi-2025"
        });
    }

    public async Task<TrackDto> GetTrackDtoAsync(Track track)
    {
        try
        {
            // Clean the TrackId: remove playlist/index params
            var cleanTrackId = track.TrackId.Split('&')[0];

            var videoRequest = _youtubeService.Videos.List("snippet");
            videoRequest.Id = cleanTrackId;
            var response = await videoRequest.ExecuteAsync();

            var video = response.Items.FirstOrDefault();
            if (video == null)
                return Fallback(track);

            return new TrackDto(
                TrackId: track.TrackId,
                Position: track.Position,
                Title: video.Snippet.Title,
                Creator: video.Snippet.ChannelTitle
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching video {track.TrackId}: {ex.Message}");
            return Fallback(track);
        }
    }

    private static TrackDto Fallback(Track track)
    {
        return new TrackDto(track.TrackId, track.Position, "Video unavailable", "");
    }
}