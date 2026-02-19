using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinecraftSkins.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToSkin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Skins",
                type: "bytea",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Skins");
        }
    }
}
