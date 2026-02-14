using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MinecraftSkins.Domain.Interfaces;
using MinecraftSkins.Domain.Models;
using MinecraftSkins.Infrastructure.Data.Extensions;

namespace MinecraftSkins.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Skin> Skins { get; set; }
    public DbSet<Purchase> Purchases { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Identity Customization: Remove unused tables ---
        // We only need Users for simple JWT auth.
        // Removing Roles, Claims, Logins, Tokens to keep schema clean.
        
        modelBuilder.Ignore<IdentityRole>();
        modelBuilder.Ignore<IdentityUserRole<string>>();
        modelBuilder.Ignore<IdentityUserClaim<string>>();
        modelBuilder.Ignore<IdentityUserLogin<string>>();
        modelBuilder.Ignore<IdentityRoleClaim<string>>();
        modelBuilder.Ignore<IdentityUserToken<string>>();

        // Customize AspNetUsers table name if desired (optional, keeping default)
        modelBuilder.Entity<IdentityUser>().ToTable("Users"); 
        // ----------------------------------------------------

        // Apply global query filter for Soft Delete
        modelBuilder.Entity<Skin>().HasQueryFilter(s => !s.IsDeleted);

        modelBuilder.Entity<Skin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BasePriceUsd).HasPrecision(18, 8); 
            entity.Property(e => e.BasePriceUsd).IsRequired();
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
        });

        modelBuilder.Seed();
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
