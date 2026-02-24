using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinecraftSkins.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UniqueIndexForPurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Purchases_BuyerId_SkinId",
                table: "Purchases",
                columns: new[] { "BuyerId", "SkinId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Purchases_BuyerId_SkinId",
                table: "Purchases");
        }
    }
}
