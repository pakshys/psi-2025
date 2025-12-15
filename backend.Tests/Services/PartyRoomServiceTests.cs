using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Database;
using backend.Exceptions;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
  public async Task GetAllAsync_WhenNoRooms_ReturnsEmptyList()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    var rooms = await service.GetAllAsync();

    Assert.NotNull(rooms);
    Assert.Empty(rooms);
  }

  [Fact]
  public async Task GetAllAsync_WhenRoomsExist_ReturnsAllRooms()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    await service.CreateAsync("Room 1", 5, false, null);
    await service.CreateAsync("Room 2", 10, false, null);
    await service.CreateAsync("Private Room", 3, true, "secret");

    var rooms = await service.GetAllAsync();

    Assert.Equal(3, rooms.Count);

    Assert.Contains(rooms, r => r.Name == "Room 1");
    Assert.Contains(rooms, r => r.Name == "Room 2");
    Assert.Contains(rooms, r => r.Name == "Private Room");
  }

  [Fact]
  public async Task GetByIdAsync_WhenRoomDoesNotExist_ThrowsNotFoundException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    await Assert.ThrowsAsync<NotFoundException>(() =>
        service.GetByIdAsync(999));
  }

  [Fact]
  public async Task GetByIdAsync_WhenRoomExists_ReturnsRoom()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    var created = await service.CreateAsync("Test Room", 5, false, null);

    var room = await service.GetByIdAsync(created.Id);

    Assert.NotNull(room);
    Assert.Equal(created.Id, room.Id);
    Assert.Equal("Test Room", room.Name);
    Assert.Equal(5, room.Capacity);
    Assert.False(room.IsPrivate);
  }

  [Fact]
  public async Task CreateAsync_WhenNameIsEmpty_ThrowsArgumentException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    await Assert.ThrowsAsync<ArgumentException>(() =>
        service.CreateAsync("", 5, false, null));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public async Task CreateAsync_WhenCapacityIsInvalid_ThrowsArgumentException(int capacity)
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    await Assert.ThrowsAsync<ArgumentException>(() =>
        service.CreateAsync("Party", capacity, false, null));
  }

  [Fact]
  public async Task CreateAsync_WhenPrivateRoomWithoutPassword_ThrowsArgumentException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    await Assert.ThrowsAsync<ArgumentException>(() =>
        service.CreateAsync("Party", 5, true, null));
  }

  [Fact]
  public async Task CreateAsync_WithValidData_CreatesRoom()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    var room = await service.CreateAsync("Party", 5, true, "secret");

    Assert.NotNull(room);
    Assert.Equal("Party", room.Name);
    Assert.Equal(5, room.Capacity);
    Assert.True(room.IsPrivate);
    Assert.NotNull(room.PasswordHash);

    var stored = await db.PartyRooms.FindAsync(room.Id);
    Assert.NotNull(stored);
  }

  [Fact]
  public async Task JoinAsync_RoomDoesNotExist_ThrowsNotFoundException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    await Assert.ThrowsAsync<NotFoundException>(() =>
        service.JoinAsync(9999, "password"));
  }

  [Fact]
  public async Task JoinAsync_PrivateRoomEmptyPassword_ThrowsUnauthorizedAccessException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    var room = await service.CreateAsync(
        name: "Private",
        capacity: 5,
        isPrivate: true,
        password: "secret");

    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
        service.JoinAsync(room.Id, null));
  }

  [Fact]
  public async Task JoinAsync_PrivateRoomInvalidPassword_ThrowsUnauthorizedAccessException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    var room = await service.CreateAsync(
        name: "Private",
        capacity: 5,
        isPrivate: true,
        password: "correct-password");

    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
        service.JoinAsync(room.Id, "wrong-password"));
  }

  [Fact]
  public async Task JoinAsync_WhenRoomIsFull_ThrowsInvalidOperationException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    var room = await service.CreateAsync("FullRoom", 1, false, null);
    room.Members = new List<string> { "User1" };
    await db.SaveChangesAsync();

    await Assert.ThrowsAsync<InvalidOperationException>(() => service.JoinAsync(room.Id, null));
  }

  [Fact]
  public async Task LeaveAsync_RoomDoesNotExist_ThrowsNotFoundException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    await Assert.ThrowsAsync<NotFoundException>(() =>
        service.LeaveAsync(9999));
  }

  [Fact]
  public async Task LeaveAsync_RoomIsEmpty_ThrowsInvalidOperationException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    var room = await service.CreateAsync("EmptyRoom", 5, false, null);

    // members list empty by default
    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        service.LeaveAsync(room.Id));
  }

  [Fact]
  public async Task UpdateAsync_EmptyName_ThrowsArgumentException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    var room = await service.CreateAsync("Room", 5, false, null);

    room.Name = "";

    await Assert.ThrowsAsync<ArgumentException>(() =>
        service.UpdateAsync(room));
  }

  [Fact]
  public async Task UpdateAsync_InvalidCapacity_ThrowsArgumentException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    var room = await service.CreateAsync("Room", 5, false, null);

    room.Capacity = 0;

    await Assert.ThrowsAsync<ArgumentException>(() =>
        service.UpdateAsync(room));
  }

  [Fact]
  public async Task UpdateAsync_CapacityLessThanCurrentMembers_ThrowsInvalidOperationException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    var room = await service.CreateAsync("Room", 5, false, null);

    room.Members = new List<string> { "user1", "user2" };
    await db.SaveChangesAsync();

    room.Capacity = 1;

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        service.UpdateAsync(room));
  }

  [Fact]
  public async Task UpdateAsync_WithValidData_UpdatesRoom()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    var room = await service.CreateAsync("OldName", 5, false, null);

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
  public async Task DeleteAsync_WhenRoomDoesntExist_NotFoundException()
  {
    var db = await GetDbContextAsync();
    var service = new PartyRoomService(db, NullLogger<PartyRoomService>.Instance);

    var room = await service.CreateAsync("ToDelete", 5, false, null);
    await service.DeleteAsync(room.Id);

    await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(room.Id));
  }
}
