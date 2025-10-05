using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
	public class UserProfile
	{
		[Key]
		public int Id { get; set; }

		[ForeignKey("User")]
		public string UserId { get; set; } = null!;
		public User User { get; set; } = null!;

		[MaxLength(100)]
		public string DisplayName { get; set; } = string.Empty;

		[MaxLength(500)]
		public string? Bio { get; set; }

		public string? ProfilePictureUrl { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
