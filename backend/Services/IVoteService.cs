public interface IVoteService
{
    void StartVote(string roomId, string action);
    bool TryCastVote(string roomId, string userId, bool agree, out int yesVotes, out int totalVotes);
    bool hasVote(string roomId);
    string? GetVoteAction(string roomId);
    int CountVotes(string roomId);
    int CountYesVotes(string roomId);
    bool RemoveVote(string roomId);
    List<(string roomId, string action)> GetExpiredVotes(double timeoutSeconds = 30);
}