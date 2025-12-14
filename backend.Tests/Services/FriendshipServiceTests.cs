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
  public async Task SendRequestAsync_RequestToSelf_ThrowsArgumentException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    await Assert.ThrowsAsync<ArgumentException>(() => 
      service.SendRequestAsync("user1", "user1"));
  }

  [Fact]
  public async Task SendRequestAsync_AlreadyFriends_ThrowsInvalidOperationException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");
    await service.AcceptRequestAsync(friendship.Id, "user2");

    await Assert.ThrowsAsync<InvalidOperationException>(() => 
      service.SendRequestAsync("user1", "user2"));
  }

  [Fact]
  public async Task SendRequestAsync_PendingRequest_ThrowsInvalidOperationException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");

    await Assert.ThrowsAsync<InvalidOperationException>(() => 
      service.SendRequestAsync("user1", "user2"));
  }

  [Fact]
  public async Task SendRequestAsync_RequestToOther_CreatesPendingFriendship()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");

    Assert.NotNull(friendship);
    Assert.Equal("user1", friendship.RequesterId);
    Assert.Equal("user2", friendship.AddresseeId);
    Assert.Equal(FriendshipStatus.Pending, friendship.Status);

    var stored = await db.Friendships.SingleAsync();
    Assert.Equal(friendship.Id, stored.Id);
  }

  [Fact]
  public async Task AcceptRequestAsync_NonExistingRequest_ThrowsKeyNotFoundException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    await Assert.ThrowsAsync<KeyNotFoundException>(() =>
      service.AcceptRequestAsync(999, "user1"));
  }

  [Fact]
  public async Task AcceptRequestAsync_AlreadyAccepted_ThrowsInvalidOperationException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");
    await service.AcceptRequestAsync(friendship.Id, "user2");

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
      service.AcceptRequestAsync(friendship.Id, "user2"));
  }

  [Fact]
  public async Task AcceptRequestAsync_UserNotAddressee_ThrowsUnauthorizedAccessException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");

    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
      service.AcceptRequestAsync(friendship.Id, "user1"));
  }

  [Fact]
  public async Task AcceptRequestAsync_ValidRequest_ChangesStatusToAccepted()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");
    await service.AcceptRequestAsync(friendship.Id, "user2");

    var updated = await db.Friendships.FindAsync(friendship.Id);

    Assert.NotNull(updated);
    Assert.Equal(FriendshipStatus.Accepted, updated!.Status);
  }

  [Fact]
  public async Task RejectRequestAsync_NonExistingRequest_ThrowsKeyNotFoundException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    await Assert.ThrowsAsync<KeyNotFoundException>(() =>
      service.RejectRequestAsync(999, "user1"));
  }

  [Fact]
  public async Task RejectRequestAsync_NotPending_ThrowsInvalidOperationException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");
    await service.AcceptRequestAsync(friendship.Id, "user2");

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
      service.RejectRequestAsync(friendship.Id, "user2"));
  }

  [Fact]
  public async Task RejectRequestAsync_UserNotAddressee_ThrowsUnauthorizedAccessException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");

    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
      service.RejectRequestAsync(friendship.Id, "user1"));
  }

  [Fact]
  public async Task AcceptRequestAsync_ValidRequest_RemovesFriendship()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");
    await service.RejectRequestAsync(friendship.Id, "user2");

    var deleted = await db.Friendships.FindAsync(friendship.Id);
    Assert.Null(deleted);
  }

  [Fact]
  public async Task GetAcceptedSummariesAsync_ReturnsFriendSummaries()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var f1 = await service.SendRequestAsync("user1", "user2");
    var f2 = await service.SendRequestAsync("user3", "user1"); // user3 has null UserName

    await service.AcceptRequestAsync(f1.Id, "user2");
    await service.AcceptRequestAsync(f2.Id, "user1");

    var summaries = await service.GetAcceptedSummariesAsync("user1");

    Assert.Equal(2, summaries.Count);

    Assert.All(summaries, s =>
        Assert.Equal(FriendshipStatus.Accepted, s.Status));

    var summary1 = summaries.Single(s => s.OtherUserId == "user2");
    Assert.Equal("Bob", summary1.OtherUserName);

    var summary2 = summaries.Single(s => s.OtherUserId == "user3");
    Assert.Equal("user3", summary2.OtherUserName); // fallback when UserName is null
  }

  [Fact]
  public async Task GetIncomingPendingSummariesAsync_ReturnsPendingSummaries()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    await service.SendRequestAsync("user1", "user2");
    await service.SendRequestAsync("user3", "user2");

    // this one shouldnt be included (outgoing)
    await service.SendRequestAsync("user2", "user4");

    var summaries = await service.GetIncomingPendingSummariesAsync("user2");

    Assert.Equal(2, summaries.Count);

    Assert.All(summaries, s =>
        Assert.Equal(FriendshipStatus.Pending, s.Status));

    Assert.Contains(summaries, s =>
        s.OtherUserId == "user1" && s.OtherUserName == "Alice");

    Assert.Contains(summaries, s =>
        s.OtherUserId == "user3" && s.OtherUserName == "user3"); // null username fallback
  }

  [Fact]
  public async Task GetOutgoingPendingSummariesAsync_ReturnsOutgoingPendingSummaries()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    await service.SendRequestAsync("user2", "user1");
    await service.SendRequestAsync("user2", "user3");

    // this one shouldnt be included (incoming)
    await service.SendRequestAsync("user4", "user2");

    var summaries = await service.GetOutgoingPendingSummariesAsync("user2");

    Assert.Equal(2, summaries.Count);

    Assert.All(summaries, s =>
        Assert.Equal(FriendshipStatus.Pending, s.Status));

    Assert.Contains(summaries, s =>
        s.OtherUserId == "user1" && s.OtherUserName == "Alice");

    Assert.Contains(summaries, s =>
        s.OtherUserId == "user3" && s.OtherUserName == "user3"); // null username fallback
  }

  [Fact]
  public async Task CancelOutgoingRequestAsync_RequestDoesntExist_ThrowsKeyNotFoundException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    await Assert.ThrowsAsync<KeyNotFoundException>(() =>
        service.CancelOutgoingRequestAsync(9999, "user1"));
  }

  [Fact]
  public async Task CancelOutgoingRequestAsync_UserNotRequester_ThrowsUnauthorizedAccessException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");

    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
        service.CancelOutgoingRequestAsync(friendship.Id, "user2"));
  }

  [Fact]
  public async Task CancelOutgoingRequestAsync_FriendshipNotPending_ThrowsInvalidOperationException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");
    await service.AcceptRequestAsync(friendship.Id, "user2");

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        service.CancelOutgoingRequestAsync(friendship.Id, "user1"));
  }

  [Fact]
  public async Task CancelOutgoingRequestAsync_ValidRequest_RemovesFriendship()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");
    await service.CancelOutgoingRequestAsync(friendship.Id, "user1");

    var exists = await db.Friendships.FindAsync(friendship.Id);
    Assert.Null(exists);
  }

  [Fact]
  public async Task RemoveFriendAsync_FriendshipDoesntExist_ThrowsKeyNotFoundException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    await Assert.ThrowsAsync<KeyNotFoundException>(() =>
        service.RemoveFriendAsync(9999, "user1"));
  }

  [Fact]
  public async Task RemoveFriendAsync_UserNotParticipant_ThrowsUnauthorizedAccessException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");
    await service.AcceptRequestAsync(friendship.Id, "user2");

    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
        service.RemoveFriendAsync(friendship.Id, "user3"));
  }

  [Fact]
  public async Task RemoveFriendAsync_FriendshipNotAccepted_ThrowsInvalidOperationException()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        service.RemoveFriendAsync(friendship.Id, "user1"));
  }

  [Fact]
  public async Task RemoveFriendAsync_ValidRequest_RemovesFriendship()
  {
    var db = await GetDbContextAsync();
    var service = new FriendshipService(db);

    var friendship = await service.SendRequestAsync("user1", "user2");
    await service.AcceptRequestAsync(friendship.Id, "user2");

    await service.RemoveFriendAsync(friendship.Id, "user1");

    var exists = await db.Friendships.FindAsync(friendship.Id);
    Assert.Null(exists);
  }
}
