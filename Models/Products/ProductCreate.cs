using System.ComponentModel.DataAnnotations;

namespace Sushi.Models.Products;

public class ProductCreate
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = default!;

    [MaxLength(160)]
    public string? Description { get; set; }

    [Range(0, 9_999_999)]
    public decimal Price { get; set; }

    [Range(0, 9_999_999)]
    public decimal? DiscountPrice { get; set; }

    [Required, MaxLength(50)]
    public string Sku { get; set; } = default!;

    [Range(0, int.MaxValue)]
    public int Stock { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public string? Slug { get; set; }
}
