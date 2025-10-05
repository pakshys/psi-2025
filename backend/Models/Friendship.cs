using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public enum FriendshipStatus
    {
        Pending,
        Accepted,
        Blocked
    }

    public class Friendship
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Requester")]
        public string RequesterId { get; set; } = null!;
        public User Requester { get; set; } = null!;

        [ForeignKey("Addressee")]
        public string AddresseeId { get; set; } = null!;
        public User Addressee { get; set; } = null!;

        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
