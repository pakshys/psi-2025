using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace backend.Database
{
    public class User : IdentityUser
    {
        [Required]
        [MaxLength(64)]
        public string Nickname { get; set; } = string.Empty;
    }
}
