using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using backend.Database;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class UserProfileServiceTests
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

  private async Task SeedUserAsync(ApplicationDbContext db, string userId, string userName)
  {
    var user = new User { Id = userId, UserName = userName };
    db.Users.Add(user);
    await db.SaveChangesAsync();
  }

  [Fact]
  public async Task CreateOrUpdateAsync_WhenProfileDoesNotExist_CreatesProfile()
  {
    var db = await GetDbContextAsync();
    await SeedUserAsync(db, "user1", "TestUser");

    var service = new UserProfileService(db);
    var profile = await service.CreateOrUpdateAsync("user1", "Display", "Bio", "pic.png");

    Assert.NotNull(profile);
    Assert.Equal("user1", profile.UserId);
    Assert.Equal("Display", profile.DisplayName);
    Assert.Equal("Bio", profile.Bio);
    Assert.Equal("pic.png", profile.ProfilePictureUrl);
  }

  [Fact]
  public async Task CreateOrUpdateAsync_WhenProfileExists_UpdatesProfile()
  {
    var db = await GetDbContextAsync();
    await SeedUserAsync(db, "user1", "TestUser");

    var existing = new UserProfile { UserId = "user1", DisplayName = "Old", Bio = "OldBio", User = db.Users.First() };
    db.UserProfiles.Add(existing);
    await db.SaveChangesAsync();

    var service = new UserProfileService(db);
    var updated = await service.CreateOrUpdateAsync("user1", "New", "NewBio", "newpic.png");

    Assert.Equal(existing.UserId, updated.UserId);
    Assert.Equal("New", updated.DisplayName);
    Assert.Equal("NewBio", updated.Bio);
    Assert.Equal("newpic.png", updated.ProfilePictureUrl);
  }

  [Fact]
  public async Task GetByUserIdAsync_WhenProfileExists_ReturnsProfile()
  {
    var db = await GetDbContextAsync();
    await SeedUserAsync(db, "user1", "TestUser");

    var profile = new UserProfile { UserId = "user1", DisplayName = "X", User = db.Users.First() };
    db.UserProfiles.Add(profile);
    await db.SaveChangesAsync();

    var service = new UserProfileService(db);
    var result = await service.GetByUserIdAsync("user1");

    Assert.NotNull(result);
    Assert.Equal("user1", result!.UserId);
  }

  [Fact]
  public async Task GetByUserIdAsync_WhenProfileDoesNotExist_ReturnsNull()
  {
    var db = await GetDbContextAsync();
    var service = new UserProfileService(db);

    var result = await service.GetByUserIdAsync("unknown");
    Assert.Null(result);
  }

  [Fact]
  public async Task GetAllAsync_ReturnsAllProfiles()
  {
    var db = await GetDbContextAsync();
    await SeedUserAsync(db, "u1", "User1");
    await SeedUserAsync(db, "u2", "User2");

    db.UserProfiles.Add(new UserProfile { UserId = "u1", DisplayName = "A", User = db.Users.First(u => u.Id == "u1") });
    db.UserProfiles.Add(new UserProfile { UserId = "u2", DisplayName = "B", User = db.Users.First(u => u.Id == "u2") });
    await db.SaveChangesAsync();

    var service = new UserProfileService(db);
    var all = await service.GetAllAsync();

    Assert.Equal(2, all.Count);
  }
}