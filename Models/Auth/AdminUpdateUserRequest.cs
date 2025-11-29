using System.ComponentModel.DataAnnotations;

namespace Sushi.Models.Auth;

public class AdminUpdateUserRequest
{
    [Required, MaxLength(100)]
    public string UserName { get; set; } = default!;

    [Required, MaxLength(256), EmailAddress]
    public string Email { get; set; } = default!;

    public bool IsAdmin { get; set; }
}
