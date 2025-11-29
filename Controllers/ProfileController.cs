using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sushi.Data;
using Sushi.Models.Auth;

namespace Sushi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly SushiDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IWebHostEnvironment _env;

    public ProfileController(
        SushiDbContext db,
        IPasswordHasher<User> passwordHasher,
        IWebHostEnvironment env)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _env = env;
    }

    private int GetUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirst("id")?.Value;

        if (string.IsNullOrWhiteSpace(idStr))
            throw new InvalidOperationException("User id claim not found.");

        return int.Parse(idStr);
    }

    private string GetAvatarRoot()
    {
        return Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "avatars");
    }

    // GET: /api/profile
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> Get()
    {
        var userId = GetUserId();

        var user = await _db.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.IsAdmin,
            user.AvatarFileName,
            user.CreatedAt
        });
    }

    // PUT: /api/profile  (ویرایش پروفایل + تصویر)
    [HttpPut]
    [RequestSizeLimit(10_000_000)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> Update(
        [FromForm] ProfileUpdateRequest dto,
        IFormFile? avatar)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userId = GetUserId();

        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return NotFound();

        var existsUserName = await _db.AppUsers
            .AnyAsync(u => u.Id != userId && u.UserName == dto.UserName);
        if (existsUserName)
            return BadRequest(new { message = "نام کاربری تکراری است." });

        var existsEmail = await _db.AppUsers
            .AnyAsync(u => u.Id != userId && u.Email == dto.Email);
        if (existsEmail)
            return BadRequest(new { message = "ایمیل قبلاً استفاده شده است." });

        user.UserName = dto.UserName.Trim();
        user.Email = dto.Email.Trim();

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        }

        if (avatar is not null && avatar.Length > 0)
        {
            var root = GetAvatarRoot();
            Directory.CreateDirectory(root);

            // حذف تصویر قبلی
            if (!string.IsNullOrWhiteSpace(user.AvatarFileName))
            {
                var oldPath = Path.Combine(root, user.AvatarFileName);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            var ext = Path.GetExtension(avatar.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(root, fileName);

            await using (var stream = System.IO.File.Create(path))
            {
                await avatar.CopyToAsync(stream);
            }

            user.AvatarFileName = fileName;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.IsAdmin,
            user.AvatarFileName,
            user.CreatedAt
        });
    }
}
