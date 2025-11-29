using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sushi.Data;
using Sushi.Models.Auth;

namespace Sushi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SushiDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _config;

    public AuthController(
        SushiDbContext db,
        IPasswordHasher<User> passwordHasher,
        IConfiguration config)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _config = config;
    }

    // POST: /api/auth/register
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var existsUserName = await _db.AppUsers
            .AnyAsync(u => u.UserName == dto.UserName);
        if (existsUserName)
            return BadRequest(new { message = "نام کاربری تکراری است." });

        var existsEmail = await _db.AppUsers
            .AnyAsync(u => u.Email == dto.Email);
        if (existsEmail)
            return BadRequest(new { message = "ایمیل قبلاً ثبت شده است." });

        // اولین کاربر → ادمین
        var isFirstUser = !await _db.AppUsers.AnyAsync();

        var user = new User
        {
            UserName = dto.UserName.Trim(),
            Email = dto.Email.Trim(),
            IsAdmin = isFirstUser
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        var response = new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            IsAdmin = user.IsAdmin
        };

        return CreatedAtAction(nameof(Me), new { }, response);
    }

    // POST: /api/auth/login
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var identifier = dto.UserNameOrEmail.Trim();

        var user = await _db.AppUsers
            .FirstOrDefaultAsync(u =>
                u.UserName == identifier || u.Email == identifier);

        if (user is null)
            return BadRequest(new { message = "نام کاربری/ایمیل یا رمز عبور نادرست است." });

        var verify = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.Password);

        if (verify == PasswordVerificationResult.Failed)
            return BadRequest(new { message = "نام کاربری/ایمیل یا رمز عبور نادرست است." });

        var token = GenerateJwtToken(user);

        var response = new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            IsAdmin = user.IsAdmin
        };

        return Ok(response);
    }

    // GET: /api/auth/me
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<object>> Me()
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                          ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _db.AppUsers.FindAsync(userId);
        if (user is null)
            return Unauthorized();

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.IsAdmin
        });
    }

    private string GenerateJwtToken(User user)
    {
        var keyString = _config["Jwt:Key"]
                        ?? "p9Z!c3P#qLm82^Gd5@wXr7$Bk1Nf4&Hs8Yz0TuV6jKoQ2eCiR%aDnLgMhJ";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
        };

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
