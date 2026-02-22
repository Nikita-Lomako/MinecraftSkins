using Microsoft.EntityFrameworkCore;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Infrastructure.Data.Extensions;

public static class ModelBuilderExtensions
{
    public static void Seed(this ModelBuilder modelBuilder)
    {
        var fixedDate = new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc);

        var skins = new[]
        {
            (Id: Guid.Parse("11111111-1111-1111-1111-111111111111"), Name: "Steve", Price: 5.00m, Available: true),
            (Id: Guid.Parse("22222222-2222-2222-2222-222222222222"), Name: "Alex", Price: 5.00m, Available: true),
            (Id: Guid.Parse("33333333-3333-3333-3333-333333333333"), Name: "Creeper", Price: 10.00m, Available: true),
            (Id: Guid.Parse("44444444-4444-4444-4444-444444444444"), Name: "Enderman", Price: 12.50m, Available: true),
            (Id: Guid.Parse("55555555-5555-5555-5555-555555555555"), Name: "Zombie", Price: 3.50m, Available: true),
            (Id: Guid.Parse("66666666-6666-6666-6666-666666666666"), Name: "Skeleton", Price: 4.00m, Available: false),
            (Id: Guid.Parse("77777777-7777-7777-7777-777777777777"), Name: "Herobrine", Price: 15.00m, Available: true),
            (Id: Guid.Parse("88888888-8888-8888-8888-888888888888"), Name: "Pig", Price: 2.00m, Available: true),
            (Id: Guid.Parse("99999999-9999-9999-9999-999999999999"), Name: "Cow", Price: 2.50m, Available: true),
            (Id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Name: "Sheep", Price: 2.25m, Available: true),
            (Id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Name: "Chicken", Price: 1.75m, Available: true),
            (Id: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Name: "Ender Dragon", Price: 25.00m, Available: true),
            (Id: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), Name: "Wither", Price: 20.00m, Available: true),
            (Id: Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), Name: "Spider", Price: 6.00m, Available: true),
            (Id: Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), Name: "Diamond Steve", Price: 10.00m, Available: true),
            (Id: Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1"), Name: "Villager", Price: 4.50m, Available: true),
            (Id: Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"), Name: "Witch", Price: 8.00m, Available: true),
            (Id: Guid.Parse("c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3"), Name: "Blaze", Price: 11.00m, Available: true),
            (Id: Guid.Parse("d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4"), Name: "Ghast", Price: 9.00m, Available: false),
            (Id: Guid.Parse("e5e5e5e5-e5e5-e5e5-e5e5-e5e5e5e5e5e5"), Name: "Slime", Price: 3.00m, Available: true),
        };

        modelBuilder.Entity<Skin>().HasData(
            skins.Select(s => new Skin
            {
                Id = s.Id,
                Name = s.Name,
                BasePriceUsd = s.Price,
                IsAvailable = s.Available,
                CreatedAtUtc = fixedDate,
                IsDeleted = false,
            }).ToArray()
        );
    }
}
