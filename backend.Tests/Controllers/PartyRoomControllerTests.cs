using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using backend;
using backend.Database;
using backend.Dtos;
using backend.Models;
using backend.Services;
using backend.Tests.Factories;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace backend.Tests
{
  public sealed class FakeTrackQueueService : ITrackQueueService
  {
    public Task<List<TrackDto>> GetTrackQueueAsync(int partyRoomId)
    {
      return Task.FromResult(new List<TrackDto>
      {
        new TrackDto(
          TrackId: "placeholder",
          Position: 0,
          Title: "No video loaded",
          Creator: "System"
        )
      });
    }

    public Task EnqueueAsync(int roomId, string trackId)
      => Task.CompletedTask;

    public Task<Track?> DequeueAsync(int roomId)
      => Task.FromResult<Track?>(null);

    public Task<Track?> PeekAsync(int roomId)
      => Task.FromResult<Track?>(null);
  }

  public class PartyRoomControllerTests
  {
    private HttpClient _client = null!;

    private async Task<TestFactory<Program>> InitializeTestClient()
    {
      var factory = new TestFactory<Program>();

      factory.ConfigureService<ITrackQueueService>(new FakeTrackQueueService());

      _client = factory.CreateClient();

      await SeedDatabase(factory);
      return factory;
    }

    private async Task SeedDatabase(TestFactory<Program> factory)
    {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<backend.Database.ApplicationDbContext>();

      // Seed initial party rooms
      db.PartyRooms.Add(new PartyRoom 
      { 
        Name = "Test Room 1",
        Capacity = 10,
        GuestsCount = 0,
        IsPrivate = false
      });
      db.PartyRooms.Add(new PartyRoom
      {
        Name = "Test Room 2",
        Capacity = 5,
        GuestsCount = 0,
        IsPrivate = true,
        PasswordHash = PasswordHelper.Hash("Secret")
      });

      await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAll_ReturnsAllRooms()
    {
      await InitializeTestClient();

      var response = await _client.GetAsync("/PartyRoom");
      response.EnsureSuccessStatusCode();

      var rooms = await response.Content.ReadFromJsonAsync<List<PartyRoom>>();
      Assert.NotNull(rooms);
      Assert.True(rooms.Count >= 2);

      Assert.Contains(rooms, r => r.Name == "Test Room 1");
      Assert.Contains(rooms, r => r.Name == "Test Room 2");
    }

    [Fact]
    public async Task Get_ReturnsRoomById()
    {
      var factory = await InitializeTestClient();
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<backend.Database.ApplicationDbContext>();
      var room = await db.PartyRooms.FirstAsync();

      var response = await _client.GetAsync($"/PartyRoom/{room.Id}");
      response.EnsureSuccessStatusCode();

      var json = await response.Content.ReadAsStringAsync();

      Assert.Contains("\"id\":", json);
      Assert.Contains(room.Name, json);
      Assert.Contains("\"queue\"", json.ToLower());
    }

    [Fact]
    public async Task Create_CreatesRoom()
    {
      var factory = await InitializeTestClient();

      var dto = new CreatePartyRoomDto(
        Name: "New Party",
        Capacity: 10,
        IsPrivate: false,
        Password: null
      );

      var response = await _client.PostAsJsonAsync("/PartyRoom", dto);

      Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

      var createdRoom = await response.Content.ReadFromJsonAsync<PartyRoom>();
      Assert.NotNull(createdRoom);
      Assert.Equal("New Party", createdRoom!.Name);
      Assert.Equal(10, createdRoom.Capacity);
      Assert.Equal(1, createdRoom.GuestsCount);

      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      var roomInDb = await db.PartyRooms.FindAsync(createdRoom.Id);
      Assert.NotNull(roomInDb);
      Assert.Equal(1, roomInDb!.GuestsCount); // creator counted as guest 1
    }

    [Fact]
    public async Task Join_WhenValid_ReturnsOkAndRoom()
    {
      var factory = await InitializeTestClient();
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      var room = await db.PartyRooms.FirstAsync(r => !r.IsPrivate);

      var response = await _client.PostAsJsonAsync(
        $"/PartyRoom/{room.Id}/join",
        new JoinPartyRoomDto(null)
      );

      response.EnsureSuccessStatusCode();

      var returnedRoom = await response.Content.ReadFromJsonAsync<PartyRoom>();
      Assert.NotNull(returnedRoom);
      Assert.Equal(room.Id, returnedRoom!.Id);
    }

    [Fact]
    public async Task Join_PrivateRoomWithCorrectPassword_ReturnsOk()
    {
      var factory = await InitializeTestClient();
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      var room = await db.PartyRooms.FirstAsync(r => r.IsPrivate);

      var response = await _client.PostAsJsonAsync(
        $"/PartyRoom/{room.Id}/join",
        new JoinPartyRoomDto("Secret")
      );

      response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Leave_WhenRoomHasMembers_ReturnsOkAndRoom()
    {
      var factory = await InitializeTestClient();
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      var room = await db.PartyRooms.FirstAsync();
      room.Members.Add("user1"); // simulate non-empty room
      await db.SaveChangesAsync();

      var response = await _client.PostAsync($"/PartyRoom/{room.Id}/leave", null);

      response.EnsureSuccessStatusCode();

      var returnedRoom = await response.Content.ReadFromJsonAsync<PartyRoom>();
      Assert.NotNull(returnedRoom);
      Assert.Equal(room.Id, returnedRoom!.Id);
    }


    [Fact]
    public async Task Update_WithValidData_UpdatesRoom()
    {
      var factory = await InitializeTestClient();
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      var room = await db.PartyRooms.FirstAsync();
      room.Name = "UpdatedName";

      var response = await _client.PutAsJsonAsync($"/PartyRoom/{room.Id}", room);

      Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

      using var verifyScope = factory.Services.CreateScope();
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var updatedRoom = await verifyDb.PartyRooms.FindAsync(room.Id);

      Assert.NotNull(updatedRoom);
      Assert.Equal("UpdatedName", updatedRoom!.Name);
    }

    [Fact]
    public async Task Delete_RemovesRoom()
    {
      var factory = await InitializeTestClient();
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<backend.Database.ApplicationDbContext>();
      var room = await db.PartyRooms.FirstAsync();

      var response = await _client.DeleteAsync($"/PartyRoom/{room.Id}");
      response.EnsureSuccessStatusCode();

      // Verify using a fresh scope
      using var verifyScope = factory.Services.CreateScope();
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<backend.Database.ApplicationDbContext>();
      var deletedRoom = await verifyDb.PartyRooms.FindAsync(room.Id);
      Assert.Null(deletedRoom);
    }
  }
}