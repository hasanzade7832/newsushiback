using Microsoft.EntityFrameworkCore;
using Sushi.Models.Products;

namespace Sushi.Data;

public class SushiDbContext : DbContext
{
    public SushiDbContext(DbContextOptions<SushiDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.DiscountPrice).HasPrecision(18, 2);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Sku).HasMaxLength(50).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(160).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
        });
    }
}
