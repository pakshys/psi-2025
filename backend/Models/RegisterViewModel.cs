using System.ComponentModel.DataAnnotations;

public class RegisterViewModel
{
    [Required]
    [DataType(DataType.Text)]
    [StringLength(64, MinimumLength=4)]
    public string UserName { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = "";
}
