using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using backend;
using backend.Models;
using backend.Services;
using backend.Database;
using Microsoft.AspNetCore.Identity;
using backend.Tests.Factories;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace backend.Tests
{
  public class FriendshipControllerTests
  {
    private HttpClient _client = null!;
    private async Task<TestFactory<Program>> InitializeTestClient()
    {
      var factory = new TestFactory<Program>(); // unique in-memory DB
      _client = factory.CreateClient();

      await SeedDatabase(factory);

      return factory;
    }

    private async Task SeedDatabase(TestFactory<Program> factory)
    {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

      var alice = new User { UserName = "alice@test.com", Email = "alice@test.com" };
      var bob = new User { UserName = "bob@test.com", Email = "bob@test.com" };

      await userManager.CreateAsync(alice, "Password123!");
      await userManager.CreateAsync(bob, "Password123!");

      await db.SaveChangesAsync();
    }

    [Fact]
    public async Task List_ReturnsOk()
    {
      var factory = await InitializeTestClient();

      _client.DefaultRequestHeaders.Authorization =
          new System.Net.Http.Headers.AuthenticationHeaderValue(FakeAuthHandler.SchemeName, "alice@test.com");

      var response = await _client.GetAsync("/Friendship/list");
      response.EnsureSuccessStatusCode();

      var friends = await response.Content.ReadFromJsonAsync<List<FriendSummary>>();
      Assert.NotNull(friends);
    }

    [Fact]
    public async Task AddFriend_SendsRequestSuccessfully()
    {
      var factory = await InitializeTestClient();

      _client.DefaultRequestHeaders.Authorization =
          new System.Net.Http.Headers.AuthenticationHeaderValue(FakeAuthHandler.SchemeName, "alice@test.com");

      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var bob = await db.Users.FirstAsync(u => u.UserName == "bob@test.com");

      var response = await _client.PostAsync($"/Friendship/add/{bob.Id}", null);
      response.EnsureSuccessStatusCode();

      var json = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
      Assert.NotNull(json);
      Assert.True(json.ContainsKey("message"));
    }

    [Fact]
    public async Task AcceptFriendRequest_WorksCorrectly()
    {
      var factory = await InitializeTestClient();

      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var service = scope.ServiceProvider.GetRequiredService<FriendshipService>();

      var alice = await db.Users.FirstAsync(u => u.UserName == "alice@test.com");
      var bob = await db.Users.FirstAsync(u => u.UserName == "bob@test.com");

      // Send friend request from Alice to Bob
      var request = await service.SendRequestAsync(alice.Id, bob.Id);

      // Authenticate as Bob
      _client.DefaultRequestHeaders.Authorization =
          new System.Net.Http.Headers.AuthenticationHeaderValue(FakeAuthHandler.SchemeName, "bob@test.com");

      var response = await _client.PostAsync($"/Friendship/accept/{request.Id}", null);
      response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task RejectFriendRequest_WorksCorrectly()
    {
      var factory = await InitializeTestClient();

      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var service = scope.ServiceProvider.GetRequiredService<FriendshipService>();

      var alice = await db.Users.FirstAsync(u => u.UserName == "alice@test.com");
      var bob = await db.Users.FirstAsync(u => u.UserName == "bob@test.com");

      // Send friend request
      var request = await service.SendRequestAsync(alice.Id, bob.Id);

      _client.DefaultRequestHeaders.Authorization =
          new System.Net.Http.Headers.AuthenticationHeaderValue(FakeAuthHandler.SchemeName, "bob@test.com");

      var response = await _client.DeleteAsync($"/Friendship/reject/{request.Id}");
      response.EnsureSuccessStatusCode();
    }
  }
}
