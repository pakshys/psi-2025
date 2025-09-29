using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class LoginViewModel
{
	[Required]
	public string Login { get; set; } = "";

	[Required]
	[DataType(DataType.Password)]
	public string Password { get; set; } = "";

	public bool RememberMe { get; set; }
}
