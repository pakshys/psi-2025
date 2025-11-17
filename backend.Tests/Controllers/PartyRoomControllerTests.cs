using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using backend;
using backend.Models;
using backend.Services;
using backend.Tests.Factories;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace backend.Tests
{
  public class PartyRoomControllerTests
  {
    private HttpClient _client = null!;

    private async Task<TestFactory<Program>> InitializeTestClient()
    {
      var factory = new TestFactory<Program>();
      _client = factory.CreateClient();

      await SeedDatabase(factory);
      return factory;
    }

    private async Task SeedDatabase(TestFactory<Program> factory)
    {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<backend.Database.ApplicationDbContext>();

      // Seed initial party rooms
      db.PartyRooms.Add(new PartyRoom { Name = "Test Room 1", Capacity = 10, GuestsCount = 0, IsPrivate = false });
      db.PartyRooms.Add(new PartyRoom { Name = "Test Room 2", Capacity = 5, GuestsCount = 0, IsPrivate = true });

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

      var returnedRoom = await response.Content.ReadFromJsonAsync<PartyRoom>();
      Assert.Equal(room.Name, returnedRoom!.Name);
    }

    [Fact]
    public async Task Create_CreatesNewRoom()
    {
      await InitializeTestClient();

      var response = await _client.PostAsync("/PartyRoom?name=NewRoom&capacity=15&isPrivate=true", null);
      response.EnsureSuccessStatusCode();

      var createdRoom = await response.Content.ReadFromJsonAsync<PartyRoom>();
      Assert.NotNull(createdRoom);
      Assert.Equal("NewRoom", createdRoom!.Name);
      Assert.Equal(1, createdRoom.GuestsCount); // creator counted as first guest
    }

    [Fact]
    public async Task Update_ChangesRoomDetails()
    {
      var factory = await InitializeTestClient();
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<backend.Database.ApplicationDbContext>();
      var room = await db.PartyRooms.FirstAsync();

      room.Name = "UpdatedName";

      var response = await _client.PutAsJsonAsync($"/PartyRoom/{room.Id}", room);
      response.EnsureSuccessStatusCode();

      // Verify using a fresh scope/DbContext
      using var verifyScope = factory.Services.CreateScope();
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<backend.Database.ApplicationDbContext>();
      var updatedRoom = await verifyDb.PartyRooms.FindAsync(room.Id);
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