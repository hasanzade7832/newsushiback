using System.ComponentModel.DataAnnotations;

namespace Sushi.Models.Auth;

public class LoginRequest
{
    [Required]
    public string UserNameOrEmail { get; set; } = default!;

    [Required]
    public string Password { get; set; } = default!;
}
