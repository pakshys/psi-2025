using backend.Database;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using System.Linq;

public class FriendshipServiceTests
{
  private async Task<ApplicationDbContext> GetDbContextAsync()
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    var db = new ApplicationDbContext(options);

    db.Users.AddRange(
        new User { Id = "user1", UserName = "Alice" },
        new User { Id = "user2", UserName = "Bob" },
        new User { Id = "user3", UserName = null }, // for null UserName test
        new User { Id = "user4", UserName = "Charlie" }
    );

    await db.SaveChangesAsync();
    return db;
  }

  [Fact]
  public async Task SendRequestAsync_CreatesPendingFriendship()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");

    Assert.Equal("user1", friendship.RequesterId);
    Assert.Equal("user2", friendship.AddresseeId);
    Assert.Equal(FriendshipStatus.Pending, friendship.Status);

    var stored = await db.Friendships.FindAsync(friendship.Id);
    Assert.NotNull(stored);
  }

  [Fact]
  public async Task SendRequestAsync_DuplicateRequest_ThrowsInvalidOperationException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    await service.SendRequestAsync("user1", "user2");

    // same direction
    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        service.SendRequestAsync("user1", "user2"));

    // reverse direction
    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        service.SendRequestAsync("user2", "user1"));
  }

  [Fact]
  public async Task SendRequestAsync_RequestSelf_ThrowsArgumentException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    await Assert.ThrowsAsync<ArgumentException>(() =>
        service.SendRequestAsync("user1", "user1"));
  }

  [Fact]
  public async Task AcceptRequestAsync_ValidRequest_UpdatesStatusToAccepted()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");
    await service.AcceptRequestAsync(friendship.Id, "user2");

    var updated = await db.Friendships.FindAsync(friendship.Id);
    Assert.Equal(FriendshipStatus.Accepted, updated!.Status);
  }

  [Fact]
  public async Task AcceptRequestAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");

    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
        service.AcceptRequestAsync(friendship.Id, "user1"));
  }

  [Fact]
  public async Task AcceptRequestAsync_NonExistent_ThrowsKeyNotFoundException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    await Assert.ThrowsAsync<KeyNotFoundException>(() =>
        service.AcceptRequestAsync(9999, "user1"));
  }

  [Fact]
  public async Task RejectRequestAsync_ValidRequest_RemovesFriendship()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");
    await service.RejectRequestAsync(friendship.Id, "user2");

    var exists = await db.Friendships.FindAsync(friendship.Id);
    Assert.Null(exists);
  }

  [Fact]
  public async Task RejectRequestAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");

    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
        service.RejectRequestAsync(friendship.Id, "user1"));
  }

  [Fact]
  public async Task RejectRequestAsync_NonExistent_ThrowsKeyNotFoundException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    await Assert.ThrowsAsync<KeyNotFoundException>(() =>
        service.RejectRequestAsync(9999, "user1"));
  }

  [Fact]
  public async Task GetFriendsAsync_ReturnsMultipleFriends()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var f1 = await service.SendRequestAsync("user1", "user2");
    var f2 = await service.SendRequestAsync("user1", "user4");
    await service.AcceptRequestAsync(f1.Id, "user2");
    await service.AcceptRequestAsync(f2.Id, "user4");

    var friends = await service.GetFriendsAsync("user1");
    Assert.Equal(2, friends.Count);
    Assert.All(friends, f => Assert.Equal(FriendshipStatus.Accepted, f.Status));
  }

  [Fact]
  public async Task GetFriendsAsync_NoFriends_ReturnsEmpty()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friends = await service.GetFriendsAsync("user3");
    Assert.Empty(friends);
  }

  [Fact]
  public async Task GetPendingAsync_ReturnsPendingFriendships()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    await service.SendRequestAsync("user1", "user2");
    await service.SendRequestAsync("user3", "user2");

    var pending = await service.GetPendingAsync("user2");
    Assert.Equal(2, pending.Count);
    Assert.All(pending, f => Assert.Equal(FriendshipStatus.Pending, f.Status));
  }

  [Fact]
  public async Task GetSummariesAsync_ReturnsCorrectSummaries_WithNullUserName()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var f1 = await service.SendRequestAsync("user1", "user3"); // user3 has null UserName
    var f2 = await service.SendRequestAsync("user4", "user1");
    await service.AcceptRequestAsync(f2.Id, "user1");

    var summaries = await service.GetSummariesAsync("user1");
    Assert.Equal(2, summaries.Count);

    var nullNameSummary = summaries.First(s => s.OtherUserId == "user3");
    Assert.Equal("user3", nullNameSummary.OtherUserName);
  }

  [Fact]
  public async Task GetPendingSummariesAsync_ReturnsCorrectPendingSummaries()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var f1 = await service.SendRequestAsync("user1", "user2");
    var f2 = await service.SendRequestAsync("user3", "user2");

    var summaries = await service.GetPendingSummariesAsync("user2");
    Assert.Equal(2, summaries.Count);
    Assert.All(summaries, s => Assert.Equal(FriendshipStatus.Pending, s.Status));
  }

  [Fact]
  public async Task GetPendingSummariesAsync_NoPending_ReturnsEmpty()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var summaries = await service.GetPendingSummariesAsync("user1");
    Assert.Empty(summaries);
  }
}
