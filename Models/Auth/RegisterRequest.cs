using System.ComponentModel.DataAnnotations;

namespace Sushi.Models.Auth;

public class RegisterRequest
{
    [Required, MaxLength(100)]
    public string UserName { get; set; } = default!;

    [Required, MaxLength(256), EmailAddress]
    public string Email { get; set; } = default!;

    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; set; } = default!;
}
