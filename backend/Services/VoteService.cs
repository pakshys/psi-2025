using backend.Models;
using System.Collections.Concurrent;

namespace backend.Services;

public class VoteService
{
    private readonly ConcurrentDictionary<string, RoomVote> _activeVotes = new();

    public void StartVote(string roomId, string action)
    {
        _activeVotes[roomId] = new RoomVote
        {
            Action = action,
            StartTime = DateTime.UtcNow
        };
    }

    public bool TryCastVote(string roomId, string userId, bool agree, out int yesVotes, out int totalVotes)
    {
        yesVotes = 0;
        totalVotes = 0;

        if (!_activeVotes.TryGetValue(roomId, out var roomVote))
            return false;

        if (!roomVote.Votes.TryAdd(userId, agree))
        {
            yesVotes = roomVote.Votes.Values.Count(v => v);
            totalVotes = roomVote.Votes.Count;
            return false;
        }

        yesVotes = roomVote.Votes.Values.Count(v => v);
        totalVotes = roomVote.Votes.Count;
        return true;
    }

    public bool hasVote(string roomId)
    {
        return _activeVotes.ContainsKey(roomId);
    }

    public string? GetVoteAction(string roomId)
    {
        if (_activeVotes.TryGetValue(roomId, out var rv))
            return rv.Action;

        return null;
    }

    public int CountVotes(string roomId)
    {
        if (_activeVotes.TryGetValue(roomId, out var rv))
            return rv.Votes.Count;

        return 0;
    }

    public int CountYesVotes(string roomId)
    {
        if (_activeVotes.TryGetValue(roomId, out var rv))
            return rv.Votes.Values.Count(v => v);


        return 0;
    }

    public bool RemoveVote(string roomId)
    {
        return _activeVotes.TryRemove(roomId, out _);
    }

    public List<(string roomId, string action)> GetExpiredVotes(double timeoutSeconds = 30)
    {
        var now = DateTime.UtcNow;

        return _activeVotes
            .Where(kv => (now - kv.Value.StartTime).TotalSeconds > timeoutSeconds)
            .Select(kv => (kv.Key, kv.Value.Action))
            .ToList();
    }
}