using Microsoft.EntityFrameworkCore;
using Sushi.Models.Auth;
using Sushi.Models.Products;

namespace Sushi.Data;

public class SushiDbContext : DbContext
{
    public SushiDbContext(DbContextOptions<SushiDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<User> AppUsers => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Product config
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

        // User config
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("AppUsers");

            e.Property(u => u.UserName)
                .HasMaxLength(100)
                .IsRequired();

            e.Property(u => u.Email)
                .HasMaxLength(256)
                .IsRequired();

            e.Property(u => u.PasswordHash)
                .IsRequired();

            e.Property(u => u.AvatarFileName)
                .HasMaxLength(260);

            e.HasIndex(u => u.UserName).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
        });
    }
}
