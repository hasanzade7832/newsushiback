using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sushi.Data;
using Sushi.Models.Products;
using System.Text.RegularExpressions;

namespace Sushi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly SushiDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProductsController(SushiDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // GET: /api/products?page=1&pageSize=20&search=...
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var q = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p => p.Name.Contains(s) || p.Sku.Contains(s));
        }

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    // GET: /api/products/5
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await _db.Products.FindAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    // GET: /api/products/by-slug/sushi-salmon
    [HttpGet("by-slug/{slug}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> GetBySlug([FromRoute] string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var cleanSlug = slug.Trim();

        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == cleanSlug);

        return product is null ? NotFound() : Ok(product);
    }

    // POST: /api/products  (فرم + فایل)
    [HttpPost]
    [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Product>> Create(
        [FromForm] ProductCreate dto,
        IFormFile? image)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var slug = string.IsNullOrWhiteSpace(dto.Slug)
            ? ToSlug(dto.Name)
            : ToSlug(dto.Slug);

        slug = await EnsureUniqueSlug(slug);

        var p = new Product
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            Price = dto.Price,
            DiscountPrice = dto.DiscountPrice,
            Sku = dto.Sku.Trim(),
            Stock = dto.Stock,
            IsActive = dto.IsActive,
            Slug = slug
        };

        // ذخیره فایل اگر ارسال شده باشد
        if (image is not null && image.Length > 0)
        {
            var uploadsRoot = GetUploadsRoot();
            Directory.CreateDirectory(uploadsRoot);

            var ext = Path.GetExtension(image.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsRoot, fileName);

            await using (var stream = System.IO.File.Create(filePath))
            {
                await image.CopyToAsync(stream);
            }

            p.ImageFileName = fileName;
        }

        _db.Products.Add(p);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = p.Id }, p);
    }

    // PUT: /api/products/5  (اختیاری: تصویر جدید)
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] ProductUpdate dto,
        IFormFile? image)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var p = await _db.Products.FindAsync(id);
        if (p is null) return NotFound();

        p.Name = dto.Name.Trim();
        p.Description = dto.Description?.Trim();
        p.Price = dto.Price;
        p.DiscountPrice = dto.DiscountPrice;
        p.Sku = dto.Sku.Trim();
        p.Stock = dto.Stock;
        p.IsActive = dto.IsActive;

        if (!string.IsNullOrWhiteSpace(dto.Slug))
        {
            var newSlug = ToSlug(dto.Slug);
            if (newSlug != p.Slug)
                p.Slug = await EnsureUniqueSlug(newSlug, excludeId: id);
        }

        // اگر عکس جدید ارسال شد
        if (image is not null && image.Length > 0)
        {
            var uploadsRoot = GetUploadsRoot();
            Directory.CreateDirectory(uploadsRoot);

            // حذف عکس قبلی اگر وجود داشت
            if (!string.IsNullOrWhiteSpace(p.ImageFileName))
            {
                var oldPath = Path.Combine(uploadsRoot, p.ImageFileName);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            var ext = Path.GetExtension(image.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsRoot, fileName);

            await using (var stream = System.IO.File.Create(filePath))
            {
                await image.CopyToAsync(stream);
            }

            p.ImageFileName = fileName;
        }

        p.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: /api/products/5
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteById(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return NotFound();

        // حذف عکس از دیسک
        if (!string.IsNullOrWhiteSpace(p.ImageFileName))
        {
            var uploadsRoot = GetUploadsRoot();
            var path = Path.Combine(uploadsRoot, p.ImageFileName);
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        _db.Products.Remove(p);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // Helpers
    private static string ToSlug(string input)
    {
        var s = input.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"[^a-z0-9\-]", "");
        s = Regex.Replace(s, @"-+", "-").Trim('-');

        return string.IsNullOrWhiteSpace(s)
            ? Guid.NewGuid().ToString("n")[..8]
            : s;
    }

    private async Task<string> EnsureUniqueSlug(string slug, int? excludeId = null)
    {
        string candidate = slug;
        var i = 1;

        while (await _db.Products.AnyAsync(p =>
                   p.Slug == candidate &&
                   (!excludeId.HasValue || p.Id != excludeId.Value)))
        {
            candidate = $"{slug}-{i++}";
        }

        return candidate;
    }

    private string GetUploadsRoot()
    {
        return Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "products");
    }
}
