using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Database;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class TrackQueueServiceTests
{
  private async Task<ApplicationDbContext> GetDbContextAsync()
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    var db = new ApplicationDbContext(options);
    await db.Database.EnsureCreatedAsync();
    return db;
  }

  [Fact]
  public async Task EnqueueAsync_WithValidRoom_AddsTrack()
  {
    var db = await GetDbContextAsync();
    var room = new PartyRoom { Name = "Room1", Capacity = 5 };
    db.PartyRooms.Add(room);
    await db.SaveChangesAsync();

    var service = new TrackQueueService(db);
    await service.EnqueueAsync(room.Id, "track1");

    var tracks = db.Tracks.Where(t => t.PartyRoomId == room.Id).ToList();
    Assert.Single(tracks);
    Assert.Equal("track1", tracks[0].TrackId);
    Assert.Equal(0, tracks[0].Position);
  }

  [Fact]
  public async Task DequeueAsync_WhenTracksExist_RemovesAndReturnsFirstTrack()
  {
    var db = await GetDbContextAsync();
    var room = new PartyRoom { Name = "Room2", Capacity = 5 };
    db.PartyRooms.Add(room);
    await db.SaveChangesAsync();

    db.Tracks.Add(new Track { PartyRoomId = room.Id, TrackId = "t1", Position = 0 });
    db.Tracks.Add(new Track { PartyRoomId = room.Id, TrackId = "t2", Position = 1 });
    await db.SaveChangesAsync();

    var service = new TrackQueueService(db);
    var first = await service.DequeueAsync(room.Id);

    Assert.Equal("t1", first!.TrackId);

    var remaining = db.Tracks.Where(t => t.PartyRoomId == room.Id).OrderBy(t => t.Position).ToList();
    Assert.Single(remaining);
    Assert.Equal(0, remaining[0].Position); // position reindexed
    Assert.Equal("t2", remaining[0].TrackId);
  }

  [Fact]
  public async Task DequeueAsync_WhenNoTracks_ReturnsNull()
  {
    var db = await GetDbContextAsync();
    var room = new PartyRoom { Name = "EmptyRoom", Capacity = 5 };
    db.PartyRooms.Add(room);
    await db.SaveChangesAsync();

    var service = new TrackQueueService(db);
    var result = await service.DequeueAsync(room.Id);

    Assert.Null(result);
  }

  [Fact]
  public async Task PeekAsync_WhenTracksExist_ReturnsFirstTrackWithoutRemoving()
  {
    var db = await GetDbContextAsync();
    var room = new PartyRoom { Name = "RoomPeek", Capacity = 5 };
    db.PartyRooms.Add(room);
    await db.SaveChangesAsync();

    db.Tracks.Add(new Track { PartyRoomId = room.Id, TrackId = "peek1", Position = 0 });
    db.Tracks.Add(new Track { PartyRoomId = room.Id, TrackId = "peek2", Position = 1 });
    await db.SaveChangesAsync();

    var service = new TrackQueueService(db);
    var first = await service.PeekAsync(room.Id);

    Assert.Equal("peek1", first!.TrackId);

    // ensure tracks are not removed
    var count = db.Tracks.Count(t => t.PartyRoomId == room.Id);
    Assert.Equal(2, count);
  }

  [Fact]
  public async Task GetTrackQueueAsync_WhenNoTracks_ReturnsPlaceholder()
  {
    var db = await GetDbContextAsync();
    var room = new PartyRoom { Name = "EmptyQueue", Capacity = 5 };
    db.PartyRooms.Add(room);
    await db.SaveChangesAsync();

    var service = new TrackQueueService(db);

    var queue = await service.GetTrackQueueAsync(room.Id);

    Assert.Single(queue);
    Assert.Equal("placeholder", queue[0].TrackId);
    Assert.Equal("No video loaded", queue[0].Title);
  }
}
