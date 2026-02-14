using Microsoft.EntityFrameworkCore;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Infrastructure.Data.Extensions;

public static class ModelBuilderExtensions
{
    public static void Seed(this ModelBuilder modelBuilder)
    {
        // Fixed date for seeding to prevent migration updates on every run
        var fixedDate = new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Skin>().HasData(
            new Skin
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Steve",
                BasePriceUsd = 5.00m,
                IsAvailable = true,
                CreatedAtUtc = fixedDate,
                IsDeleted = false
            },
            new Skin
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Alex",
                BasePriceUsd = 5.00m,
                IsAvailable = true,
                CreatedAtUtc = fixedDate,
                IsDeleted = false
            },
            new Skin
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Creeper",
                BasePriceUsd = 10.00m,
                IsAvailable = true,
                CreatedAtUtc = fixedDate,
                IsDeleted = false
            },
            new Skin
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Enderman",
                BasePriceUsd = 12.50m,
                IsAvailable = true,
                CreatedAtUtc = fixedDate,
                IsDeleted = false
            },
            new Skin
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "Zombie",
                BasePriceUsd = 3.50m,
                IsAvailable = true,
                CreatedAtUtc = fixedDate,
                IsDeleted = false
            },
            new Skin
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Name = "Skeleton",
                BasePriceUsd = 4.00m,
                IsAvailable = false, // Not available example
                CreatedAtUtc = fixedDate,
                IsDeleted = false
            }
        );
    }
}
