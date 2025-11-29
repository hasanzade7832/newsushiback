namespace Sushi.Models.Auth;

public class AuthResponse
{
    public string Token { get; set; } = default!;

    public int UserId { get; set; }
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool IsAdmin { get; set; }
}
