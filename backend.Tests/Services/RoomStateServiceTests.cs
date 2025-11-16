using System;
using System.Linq;
using System.Collections.Generic;
using backend.Models;
using backend.Services;
using Xunit;

public class RoomStateServiceTests
{
  [Fact]
  public void SetPlayback_WithValidRoom_SetsPlaybackState()
  {
    var service = new RoomStateService();
    var state = new RoomPlaybackState("vid1", 0, true, DateTime.UtcNow);

    service.SetPlayback("room1", state);

    Assert.True(service.HasPlayback("room1"));
    Assert.Equal(state, service.GetPlayback("room1"));
  }

  [Fact]
  public void TryGetPlayback_WhenPlaybackExists_ReturnsTrueAndState()
  {
    var service = new RoomStateService();
    var state = new RoomPlaybackState("vid1", 0, false, DateTime.UtcNow);
    service.SetPlayback("room1", state);

    var result = service.TryGetPlayback("room1", out var retrieved);

    Assert.True(result);
    Assert.Equal(state, retrieved);
  }

  [Fact]
  public void TryGetPlayback_WhenPlaybackDoesNotExist_ReturnsFalse()
  {
    var service = new RoomStateService();

    var result = service.TryGetPlayback("roomX", out var retrieved);

    Assert.False(result);
    Assert.Null(retrieved);
  }

  [Fact]
  public void AddUserToRoom_NewRoom_UserIsAdded()
  {
    var service = new RoomStateService();

    service.AddUserToRoom(1, "user1");

    var users = service.GetUsersInRoom(1);
    Assert.Single(users);
    Assert.Contains("user1", users);
    Assert.True(service.RoomExists(1));
  }

  [Fact]
  public void AddUserToRoom_UserAlreadyExists_DoesNotDuplicate()
  {
    var service = new RoomStateService();
    service.AddUserToRoom(1, "user1");

    service.AddUserToRoom(1, "user1");

    var users = service.GetUsersInRoom(1);
    Assert.Single(users); // still only one
  }

  [Fact]
  public void RemoveUserFromRoom_WhenUserExists_RemovesUser()
  {
    var service = new RoomStateService();
    service.AddUserToRoom(1, "user1");

    var removed = service.RemoveUserFromRoom(1, "user1");

    Assert.True(removed);
    Assert.Empty(service.GetUsersInRoom(1));
  }

  [Fact]
  public void RemoveUserFromRoom_WhenUserDoesNotExist_ReturnsFalse()
  {
    var service = new RoomStateService();
    service.AddUserToRoom(1, "user1");

    var removed = service.RemoveUserFromRoom(1, "userX");

    Assert.False(removed);
    Assert.Single(service.GetUsersInRoom(1));
  }

  [Fact]
  public void RemoveUserFromAllRooms_UserInMultipleRooms_RemovesFromAll()
  {
    var service = new RoomStateService();
    service.AddUserToRoom(1, "user1");
    service.AddUserToRoom(2, "user1");
    service.AddUserToRoom(3, "user2");

    var removedRooms = service.RemoveUserFromAllRooms("user1");

    Assert.Equal(2, removedRooms.Count);
    Assert.DoesNotContain("user1", service.GetUsersInRoom(1));
    Assert.DoesNotContain("user1", service.GetUsersInRoom(2));
    Assert.Contains("user2", service.GetUsersInRoom(3));
  }

  [Fact]
  public void GetUsersInRoom_WhenRoomDoesNotExist_ReturnsEmptyList()
  {
    var service = new RoomStateService();

    var users = service.GetUsersInRoom(999);

    Assert.Empty(users);
  }

  [Fact]
  public void RoomExists_WhenRoomCreated_ReturnsTrue()
  {
    var service = new RoomStateService();
    service.AddUserToRoom(5, "user1");

    Assert.True(service.RoomExists(5));
  }

  [Fact]
  public void RoomExists_WhenRoomDoesNotExist_ReturnsFalse()
  {
    var service = new RoomStateService();

    Assert.False(service.RoomExists(123));
  }
}