using System.ComponentModel.DataAnnotations;

namespace Sushi.Models.Auth;

public class ProfileUpdateRequest
{
    [Required, MaxLength(100)]
    public string UserName { get; set; } = default!;

    [Required, MaxLength(256), EmailAddress]
    public string Email { get; set; } = default!;

    [MaxLength(100)]
    public string? NewPassword { get; set; }
}
