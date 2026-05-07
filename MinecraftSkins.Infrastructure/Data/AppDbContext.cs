using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MinecraftSkins.Domain.Interfaces;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Skin> Skins { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<SearchQueryHistory> SearchQueryHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Identity: minimal schema (Users, Roles, UserRoles only). ---
        // Ignore Claims/Logins/Tokens so those tables are not created.
        // Warning "mapped explicitly then ignored" is expected: base maps them first, we then ignore.
        modelBuilder.Ignore<IdentityUserClaim<string>>();
        modelBuilder.Ignore<IdentityUserLogin<string>>();
        modelBuilder.Ignore<IdentityRoleClaim<string>>();
        modelBuilder.Ignore<IdentityUserToken<string>>();

        // Customize Identity tables name
        modelBuilder.Entity<IdentityUser>().ToTable("Users"); 
        modelBuilder.Entity<IdentityRole>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        // ----------------------------------------------------

        // Apply global query filter for Soft Delete
        modelBuilder.Entity<Skin>().HasQueryFilter(s => !s.IsDeleted);

        modelBuilder.Entity<Skin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BasePriceUsd).HasPrecision(18, 8); 
            entity.Property(e => e.BasePriceUsd).IsRequired();
            
            // Unique constraint on Name to prevent duplicates
            entity.HasIndex(e => e.Name).IsUnique();
            
            // Optimistic Concurrency with RowVersion (using Fluent API instead of [Timestamp] attribute)
            // IsRowVersion() automatically sets the property as a concurrency token
            entity.Property(e => e.RowVersion)
                .IsRowVersion();
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PriceUsdFinal).HasPrecision(18, 2);
            entity.Property(e => e.BtcUsdRate).HasPrecision(18, 8); 
            entity.Property(e => e.BuyerId).IsRequired();
            
            // Relationship
            entity.HasOne(p => p.Skin)
                  .WithMany()
                  .HasForeignKey(p => p.SkinId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // Unique constraint: each user can buy each skin only once
            entity.HasIndex(p => new { p.BuyerId, p.SkinId })
                  .IsUnique();

            entity.HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(p => p.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.BuyerId).IsRequired();

            entity.HasIndex(c => c.BuyerId).IsUnique();
            entity.HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(c => c.BuyerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(ci => ci.Id);
            entity.Property(ci => ci.Quantity).IsRequired();
            entity.Property(ci => ci.UnitPriceUsd).HasPrecision(18, 2);

            entity.ToTable(t => t.HasCheckConstraint("CK_CartItems_Quantity", "\"Quantity\" > 0"));
            entity.HasIndex(ci => new { ci.CartId, ci.SkinId }).IsUnique();

            entity.HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ci => ci.Skin)
                .WithMany()
                .HasForeignKey(ci => ci.SkinId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SearchQueryHistory>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.QueryText).HasMaxLength(256).IsRequired();
            entity.Property(h => h.Endpoint).HasMaxLength(64).IsRequired();
            entity.Property(h => h.CreatedAtUtc).IsRequired();

            entity.HasIndex(h => h.CreatedAtUtc);
            entity.HasIndex(h => h.UserId);
        });

        // Seed Roles
        var adminRoleId = "admin-role-id-seed";
        var userRoleId = "user-role-id-seed";
        
        modelBuilder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole { Id = userRoleId, Name = "User", NormalizedName = "USER" }
        );       

    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            switch (entry.State)
            {
                case EntityState.Deleted:
                    entry.State = EntityState.Unchanged; // Prevent hard delete
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAtUtc = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
