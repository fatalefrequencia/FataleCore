using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FataleCore.Migrations
{
    /// <inheritdoc />
    public partial class AddWallpaperVideoUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WallpaperVideoUrl",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WallpaperVideoUrl",
                table: "Users");
        }
    }
}
