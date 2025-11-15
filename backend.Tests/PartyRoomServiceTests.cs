using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Database;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class PartyRoomServiceTests
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
  public async Task CreateAsync_WithValidData_CreatesRoom()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db);

    var room = await service.CreateAsync("Party", 5);

    Assert.NotNull(room);
    Assert.Equal("Party", room.Name);
    Assert.Equal(5, room.Capacity);
    Assert.False(room.IsPrivate);
  }

  [Fact]
  public async Task CreateAsync_WithEmptyName_ThrowsArgumentException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db);

    await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(""));
  }

  [Fact]
  public async Task JoinAsync_WhenRoomIsFull_ThrowsInvalidOperationException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db);

    var room = await service.CreateAsync("FullRoom", 1);
    room.Members = new List<string> { "User1" };
    await db.SaveChangesAsync();

    await Assert.ThrowsAsync<InvalidOperationException>(() => service.JoinAsync(room.Id));
  }

  [Fact]
  public async Task LeaveAsync_WhenRoomIsEmpty_ThrowsInvalidOperationException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db);

    var room = await service.CreateAsync("EmptyRoom", 2);

    await Assert.ThrowsAsync<InvalidOperationException>(() => service.LeaveAsync(room.Id));
  }

  [Fact]
  public async Task UpdateAsync_WithValidData_UpdatesRoom()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db);

    var room = await service.CreateAsync("OldName", 5);
    room.Members = new List<string> { "User1" };
    await db.SaveChangesAsync();

    room.Name = "NewName";
    room.Capacity = 10;
    await service.UpdateAsync(room);

    var updated = await service.GetByIdAsync(room.Id);
    Assert.Equal("NewName", updated!.Name);
    Assert.Equal(10, updated.Capacity);
  }

  [Fact]
  public async Task DeleteAsync_WhenRoomExists_DeletesRoom()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db);

    var room = await service.CreateAsync("ToDelete", 5);
    await service.DeleteAsync(room.Id);

    var deleted = await service.GetByIdAsync(room.Id);
    Assert.Null(deleted);
  }
}
