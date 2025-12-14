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
    public async Task List_UserNotFound_ReturnsUnauthorized()
    {
        await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "nonexistent@test.com"
            );

        var response = await _client.GetAsync("/Friendship/list");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_UserExists_ReturnsOkWithData()
    {
        await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        var response = await _client.GetAsync("/Friendship/list");

        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<List<FriendSummary>>();

        Assert.NotNull(data);
        Assert.Empty(data);
    }

    [Fact]
    public async Task Pending_UserNotFound_ReturnsUnauthorized()
    {
        await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "nonexistent@test.com"
            );

        var response = await _client.GetAsync("/Friendship/pending");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Pending_UserExists_ReturnsOkWithData()
    {
        await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        var response = await _client.GetAsync("/Friendship/pending");

        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<List<FriendSummary>>();

        Assert.NotNull(data);
        Assert.Empty(data); // no pending requests seeded yet
    }

    [Fact]
    public async Task Outgoing_UserNotFound_ReturnsUnauthorized()
    {
      await InitializeTestClient();

      _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue(
          FakeAuthHandler.SchemeName,
          "nonexistent@test.com"
        );

      var response = await _client.GetAsync("/Friendship/outgoing");

      Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Outgoing_UserExists_ReturnsOkWithData()
    {
      await InitializeTestClient();

      _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue(
          FakeAuthHandler.SchemeName,
          "alice@test.com"
        );

      var response = await _client.GetAsync("/Friendship/outgoing");

      response.EnsureSuccessStatusCode();

      var data = await response.Content.ReadFromJsonAsync<List<FriendSummary>>();

      Assert.NotNull(data);
      Assert.Empty(data); // no outgoing requests seeded
    }

    [Fact]
    public async Task AddByUsername_UserNull_ReturnsUnauthorized()
    {
        await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "nonexistent@test.com"
            );

        var response = await _client.PostAsync("/Friendship/add/by-username/bob@test.com", null);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddByUsername_UsernameWhitespace_ReturnsBadRequest()
    {
        await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        var response = await _client.PostAsync("/Friendship/add/by-username/%20", null);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddByUsername_TargetNotFound_ReturnsNotFound()
    {
        await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        var response = await _client.PostAsync(
            "/Friendship/add/by-username/ghost@test.com",
            null
        );

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddByUsername_ArgumentException_ReturnsBadRequest()
    {
        var factory = await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        // adding yourself for exception
        var response = await _client.PostAsync(
            "/Friendship/add/by-username/alice@test.com",
            null
        );

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddByUsername_InvalidOperation_ReturnsConflict()
    {
        var factory = await InitializeTestClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IFriendshipService>();

        var alice = await db.Users.FirstAsync(u => u.UserName == "alice@test.com");
        var bob = await db.Users.FirstAsync(u => u.UserName == "bob@test.com");

        // first request
        await service.SendRequestAsync(alice.Id, bob.Id);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        // second request causes conflict
        var response = await _client.PostAsync(
            "/Friendship/add/by-username/bob@test.com",
            null
        );

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Accept_UserNull_ReturnsUnauthorized()
    {
        await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "ghost@test.com"
            );

        var response = await _client.PostAsync("/Friendship/accept/1", null);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Accept_ValidRequest_ReturnsOk()
    {
        var factory = await InitializeTestClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IFriendshipService>();

        var alice = await db.Users.FirstAsync(u => u.UserName == "alice@test.com");
        var bob = await db.Users.FirstAsync(u => u.UserName == "bob@test.com");

        var request = await service.SendRequestAsync(alice.Id, bob.Id);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "bob@test.com"
            );

        var response = await _client.PostAsync(
            $"/Friendship/accept/{request.Id}",
            null
        );

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Accept_UnauthorizedUser_ReturnsForbidden()
    {
        var factory = await InitializeTestClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IFriendshipService>();

        var alice = await db.Users.FirstAsync(u => u.UserName == "alice@test.com");
        var bob = await db.Users.FirstAsync(u => u.UserName == "bob@test.com");

        var request = await service.SendRequestAsync(alice.Id, bob.Id);

        // attempt accept own request
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        var response = await _client.PostAsync(
            $"/Friendship/accept/{request.Id}",
            null
        );

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Accept_RequestNotFound_ReturnsNotFound()
    {
        await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        var response = await _client.PostAsync(
            "/Friendship/accept/99999",
            null
        );

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_UserNull_ReturnsUnauthorized()
    {
        await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "ghost@test.com"
            );

        var response = await _client.DeleteAsync("/Friendship/cancel/1");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_ValidOutgoingRequest_ReturnsOk()
    {
        var factory = await InitializeTestClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IFriendshipService>();

        var alice = await db.Users.FirstAsync(u => u.UserName == "alice@test.com");
        var bob = await db.Users.FirstAsync(u => u.UserName == "bob@test.com");

        var request = await service.SendRequestAsync(alice.Id, bob.Id);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        var response = await _client.DeleteAsync(
            $"/Friendship/cancel/{request.Id}"
        );

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Cancel_UnauthorizedUser_ReturnsForbidden()
    {
        var factory = await InitializeTestClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IFriendshipService>();

        var alice = await db.Users.FirstAsync(u => u.UserName == "alice@test.com");
        var bob = await db.Users.FirstAsync(u => u.UserName == "bob@test.com");

        var request = await service.SendRequestAsync(alice.Id, bob.Id);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "bob@test.com"
            );

        var response = await _client.DeleteAsync(
            $"/Friendship/cancel/{request.Id}"
        );

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_AlreadyHandledRequest_ReturnsBadRequest()
    {
        var factory = await InitializeTestClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IFriendshipService>();

        var alice = await db.Users.FirstAsync(u => u.UserName == "alice@test.com");
        var bob = await db.Users.FirstAsync(u => u.UserName == "bob@test.com");

        var request = await service.SendRequestAsync(alice.Id, bob.Id);
        await service.AcceptRequestAsync(request.Id, bob.Id);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        var response = await _client.DeleteAsync(
            $"/Friendship/cancel/{request.Id}"
        );

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_RequestNotFound_ReturnsNotFound()
    {
        await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        var response = await _client.DeleteAsync("/Friendship/cancel/99999");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Reject_UserNull_ReturnsUnauthorized()
    {
        await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "ghost@test.com"
            );

        var response = await _client.DeleteAsync("/Friendship/reject/1");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reject_ValidRequest_ReturnsOk()
    {
        var factory = await InitializeTestClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IFriendshipService>();

        var alice = await db.Users.FirstAsync(u => u.UserName == "alice@test.com");
        var bob = await db.Users.FirstAsync(u => u.UserName == "bob@test.com");

        var request = await service.SendRequestAsync(alice.Id, bob.Id);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "bob@test.com"
            );

        var response = await _client.DeleteAsync(
            $"/Friendship/reject/{request.Id}"
        );

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Reject_NotAddressee_ReturnsForbidden()
    {
        var factory = await InitializeTestClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IFriendshipService>();

        var alice = await db.Users.FirstAsync(u => u.UserName == "alice@test.com");
        var bob = await db.Users.FirstAsync(u => u.UserName == "bob@test.com");

        var request = await service.SendRequestAsync(alice.Id, bob.Id);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        var response = await _client.DeleteAsync(
            $"/Friendship/reject/{request.Id}"
        );

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Reject_RequestNotFound_ReturnsNotFound()
    {
        await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        var response = await _client.DeleteAsync("/Friendship/reject/99999");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Remove_UserNull_ReturnsUnauthorized()
    {
        await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "ghost@test.com"
            );

        var response = await _client.DeleteAsync("/Friendship/remove/1");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Remove_AcceptedFriendship_ReturnsOk()
    {
        var factory = await InitializeTestClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IFriendshipService>();

        var alice = await db.Users.FirstAsync(u => u.UserName == "alice@test.com");
        var bob = await db.Users.FirstAsync(u => u.UserName == "bob@test.com");

        var request = await service.SendRequestAsync(alice.Id, bob.Id);
        await service.AcceptRequestAsync(request.Id, bob.Id);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        var response = await _client.DeleteAsync(
            $"/Friendship/remove/{request.Id}"
        );

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Remove_UserNotParticipant_ReturnsForbidden()
    {
        var factory = await InitializeTestClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IFriendshipService>();

        var alice = await db.Users.FirstAsync(u => u.UserName == "alice@test.com");
        var bob = await db.Users.FirstAsync(u => u.UserName == "bob@test.com");

        var request = await service.SendRequestAsync(alice.Id, bob.Id);
        await service.AcceptRequestAsync(request.Id, bob.Id);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "someoneelse@test.com"
            );

        var response = await _client.DeleteAsync(
            $"/Friendship/remove/{request.Id}"
        );

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Remove_NotAcceptedFriendship_ReturnsBadRequest()
    {
        var factory = await InitializeTestClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IFriendshipService>();

        var alice = await db.Users.FirstAsync(u => u.UserName == "alice@test.com");
        var bob = await db.Users.FirstAsync(u => u.UserName == "bob@test.com");

        var request = await service.SendRequestAsync(alice.Id, bob.Id);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        var response = await _client.DeleteAsync(
            $"/Friendship/remove/{request.Id}"
        );

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Remove_FriendshipNotFound_ReturnsNotFound()
    {
        await InitializeTestClient();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                "alice@test.com"
            );

        var response = await _client.DeleteAsync("/Friendship/remove/99999");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
  }
}
