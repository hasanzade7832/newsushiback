using System.ComponentModel.DataAnnotations;

namespace Sushi.Models.Auth;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string UserName { get; set; } = default!;

    [Required, MaxLength(256), EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string PasswordHash { get; set; } = default!;

    public bool IsAdmin { get; set; }

    [MaxLength(260)]
    public string? AvatarFileName { get; set; }   // 👈 تصویر پروفایل

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
