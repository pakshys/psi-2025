using backend.Models;
using backend.Services;
using System;
using Xunit;

namespace backend.Tests.Services
{
  public class VoteServiceTests
  {
    private readonly VoteService _voteService;

    public VoteServiceTests()
    {
      _voteService = new VoteService();
    }

    [Fact]
    public void StartVote_ShouldCreateVoteForRoom()
    {
      string roomId = "room1";
      string action = "kick";

      _voteService.StartVote(roomId, action);

      Assert.True(_voteService.hasVote(roomId));
      Assert.Equal(action, _voteService.GetVoteAction(roomId));
    }

    [Fact]
    public void TryCastVote_ShouldReturnFalse_IfVoteDoesNotExist()
    {
      string roomId = "nonexistent";
      bool result = _voteService.TryCastVote(roomId, "user1", true, out int yesVotes, out int totalVotes);

      Assert.False(result);
      Assert.Equal(0, yesVotes);
      Assert.Equal(0, totalVotes);
    }

    [Fact]
    public void TryCastVote_ShouldAddVoteAndCountCorrectly()
    {
      string roomId = "room2";
      string userId = "user1";
      _voteService.StartVote(roomId, "skip");

      bool added = _voteService.TryCastVote(roomId, userId, true, out int yesVotes, out int totalVotes);

      Assert.True(added);
      Assert.Equal(1, yesVotes);
      Assert.Equal(1, totalVotes);

      // Adding a second user
      _voteService.TryCastVote(roomId, "user2", false, out yesVotes, out totalVotes);
      Assert.Equal(1, yesVotes);
      Assert.Equal(2, totalVotes);
    }

    [Fact]
    public void TryCastVote_ShouldNotAllowDuplicateUserVote()
    {
      string roomId = "room3";
      string userId = "user1";
      _voteService.StartVote(roomId, "ban");

      _voteService.TryCastVote(roomId, userId, true, out _, out _);

      bool result = _voteService.TryCastVote(roomId, userId, false, out int yesVotes, out int totalVotes);

      Assert.False(result);
      Assert.Equal(1, yesVotes);  // original vote remains
      Assert.Equal(1, totalVotes);
    }

    [Fact]
    public void CountVotes_ShouldReturnCorrectTotal()
    {
      string roomId = "room4";
      _voteService.StartVote(roomId, "mute");

      _voteService.TryCastVote(roomId, "user1", true, out _, out _);
      _voteService.TryCastVote(roomId, "user2", false, out _, out _);

      Assert.Equal(2, _voteService.CountVotes(roomId));
      Assert.Equal(1, _voteService.CountYesVotes(roomId));
    }

    [Fact]
    public void RemoveVote_ShouldRemoveVoteSuccessfully()
    {
      string roomId = "room5";
      _voteService.StartVote(roomId, "pause");

      Assert.True(_voteService.hasVote(roomId));
      Assert.True(_voteService.RemoveVote(roomId));
      Assert.False(_voteService.hasVote(roomId));
    }

    [Fact]
    public void GetExpiredVotes_ShouldReturnOnlyExpiredVotes()
    {
      string roomId1 = "r1";
      string roomId2 = "r2";
      _voteService.StartVote(roomId1, "action1");
      _voteService.StartVote(roomId2, "action2");

      // Manually modify StartTime to simulate expiration
      var expiredVote = typeof(VoteService)
          .GetField("_activeVotes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          ?.GetValue(_voteService) as System.Collections.Concurrent.ConcurrentDictionary<string, RoomVote>;

      if (expiredVote != null)
      {
        expiredVote[roomId1].StartTime = DateTime.UtcNow.AddSeconds(-31);
      }

      var expired = _voteService.GetExpiredVotes();
      Assert.Single(expired);
      Assert.Equal(roomId1, expired[0].roomId);
    }
  }
}
