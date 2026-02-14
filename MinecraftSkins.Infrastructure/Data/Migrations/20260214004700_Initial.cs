using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MinecraftSkins.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Skins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BasePriceUsd = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkinId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceUsdFinal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BtcUsdRate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    PurchasedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BuyerId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Purchases_Skins_SkinId",
                        column: x => x.SkinId,
                        principalTable: "Skins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Skins",
                columns: new[] { "Id", "BasePriceUsd", "CreatedAtUtc", "DeletedAtUtc", "IsAvailable", "IsDeleted", "Name", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 5.00m, new DateTime(2026, 2, 14, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Steve", null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 5.00m, new DateTime(2026, 2, 14, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Alex", null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 10.00m, new DateTime(2026, 2, 14, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Creeper", null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), 12.50m, new DateTime(2026, 2, 14, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Enderman", null },
                    { new Guid("55555555-5555-5555-5555-555555555555"), 3.50m, new DateTime(2026, 2, 14, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Zombie", null },
                    { new Guid("66666666-6666-6666-6666-666666666666"), 4.00m, new DateTime(2026, 2, 14, 0, 0, 0, 0, DateTimeKind.Utc), null, false, false, "Skeleton", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_SkinId",
                table: "Purchases",
                column: "SkinId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Skins");
        }
    }
}
