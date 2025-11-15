using System;
using System.Linq;
using System.Threading.Tasks;
using backend.Services;
using Xunit;

public class VoteServiceTests
{
  [Fact]
  public void StartVote_WhenCalled_AddsVoteToActiveVotes()
  {
    var service = new VoteService();

    service.StartVote("room1", "Skip");

    Assert.True(service.hasVote("room1"));
    Assert.Equal("Skip", service.GetVoteAction("room1"));
  }

  [Fact]
  public void TryCastVote_WhenNoActiveVote_ReturnsFalse()
  {
    var service = new VoteService();

    var result = service.TryCastVote("room1", "user1", true, out int yes, out int total);

    Assert.False(result);
    Assert.Equal(0, yes);
    Assert.Equal(0, total);
  }

  [Fact]
  public void TryCastVote_WhenFirstVote_AddsVoteAndUpdatesCounts()
  {
    var service = new VoteService();
    service.StartVote("room1", "Skip");

    var result = service.TryCastVote("room1", "user1", true, out int yes, out int total);

    Assert.True(result);
    Assert.Equal(1, yes);
    Assert.Equal(1, total);
  }

  [Fact]
  public void TryCastVote_WhenDuplicateVote_ReturnsFalseButCountsUpdated()
  {
    var service = new VoteService();
    service.StartVote("room1", "Skip");

    service.TryCastVote("room1", "user1", true, out _, out _);
    var result = service.TryCastVote("room1", "user1", false, out int yes, out int total);

    Assert.False(result);
    Assert.Equal(1, yes);
    Assert.Equal(1, total);
  }

  [Fact]
  public void hasVote_ReturnsTrueIfVoteExists_FalseOtherwise()
  {
    var service = new VoteService();
    Assert.False(service.hasVote("room1"));

    service.StartVote("room1", "Play");
    Assert.True(service.hasVote("room1"));
  }

  [Fact]
  public void GetVoteAction_ReturnsActionOrNull()
  {
    var service = new VoteService();
    Assert.Null(service.GetVoteAction("room1"));

    service.StartVote("room1", "Pause");
    Assert.Equal("Pause", service.GetVoteAction("room1"));
  }

  [Fact]
  public void CountVotes_ReturnsCorrectCount()
  {
    var service = new VoteService();
    service.StartVote("room1", "Skip");

    service.TryCastVote("room1", "user1", true, out _, out _);
    service.TryCastVote("room1", "user2", false, out _, out _);

    Assert.Equal(2, service.CountVotes("room1"));
    Assert.Equal(0, service.CountVotes("room2")); // non-existent room
  }

  [Fact]
  public void CountYesVotes_ReturnsCorrectCount()
  {
    var service = new VoteService();
    service.StartVote("room1", "Skip");

    service.TryCastVote("room1", "user1", true, out _, out _);
    service.TryCastVote("room1", "user2", false, out _, out _);

    Assert.Equal(1, service.CountYesVotes("room1"));
    Assert.Equal(0, service.CountYesVotes("room2")); // non-existent room
  }

  [Fact]
  public void RemoveVote_RemovesVoteSuccessfully()
  {
    var service = new VoteService();
    service.StartVote("room1", "Play");

    var removed = service.RemoveVote("room1");

    Assert.True(removed);
    Assert.False(service.hasVote("room1"));
  }
}
