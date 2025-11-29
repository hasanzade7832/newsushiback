using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sushi.Data;
using Sushi.Models.Auth;

namespace Sushi.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly SushiDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AdminUsersController(
        SushiDbContext db,
        IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    // GET: /api/admin/users
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var users = await _db.AppUsers
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.IsAdmin,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    // POST: /api/admin/users  (ساخت کاربر جدید توسط ادمین)
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> Create([FromBody] AdminCreateUserRequest dto)
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

        var user = new User
        {
            UserName = dto.UserName.Trim(),
            Email = dto.Email.Trim(),
            IsAdmin = dto.IsAdmin
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = user.Id }, new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.IsAdmin,
            user.CreatedAt
        });
    }

    // PUT: /api/admin/users/{id}/role  (تغییر نقش سریع)
    [HttpPut("{id:int}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(
        int id,
        [FromBody] UpdateUserRoleRequest dto)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user is null) return NotFound();

        user.IsAdmin = dto.IsAdmin;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // PUT: /api/admin/users/{id}  (ویرایش نام کاربری/ایمیل/نقش)
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> Update(
        int id,
        [FromBody] AdminUpdateUserRequest dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _db.AppUsers.FindAsync(id);
        if (user is null) return NotFound();

        var existsUserName = await _db.AppUsers
            .AnyAsync(u => u.Id != id && u.UserName == dto.UserName);
        if (existsUserName)
            return BadRequest(new { message = "نام کاربری تکراری است." });

        var existsEmail = await _db.AppUsers
            .AnyAsync(u => u.Id != id && u.Email == dto.Email);
        if (existsEmail)
            return BadRequest(new { message = "ایمیل قبلاً ثبت شده است." });

        user.UserName = dto.UserName.Trim();
        user.Email = dto.Email.Trim();
        user.IsAdmin = dto.IsAdmin;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.IsAdmin,
            user.CreatedAt
        });
    }

    // DELETE: /api/admin/users/{id}
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user is null) return NotFound();

        _db.AppUsers.Remove(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
