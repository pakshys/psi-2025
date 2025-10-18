namespace backend.Models
{
	public readonly record struct FriendSummary(
		int FriendshipId,
		string OtherUserId,
		string OtherUserName,
		FriendshipStatus Status,
		DateTime CreatedAt
	);
}
