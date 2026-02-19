using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinecraftSkins.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToSkinName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Skins_Name",
                table: "Skins",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Skins_Name",
                table: "Skins");
        }
    }
}
