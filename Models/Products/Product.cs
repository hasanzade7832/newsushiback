using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sushi.Models.Products;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = default!;

    [Required, MaxLength(160)]
    public string Slug { get; set; } = default!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 9_999_999)]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 9_999_999)]
    public decimal? DiscountPrice { get; set; }

    [Required, MaxLength(50)]
    public string Sku { get; set; } = default!;

    [Range(0, int.MaxValue)]
    public int Stock { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    [MaxLength(260)]
    public string? ImageFileName { get; set; }   // 👈 اسم فایل عکس

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
