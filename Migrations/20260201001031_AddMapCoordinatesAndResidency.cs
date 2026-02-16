using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FataleCore.Migrations
{
    /// <inheritdoc />
    public partial class AddMapCoordinatesAndResidency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResidentSectorId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MapX",
                table: "Tracks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MapY",
                table: "Tracks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectorId",
                table: "Tracks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MapX",
                table: "Artists",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MapY",
                table: "Artists",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectorId",
                table: "Artists",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MapX",
                table: "Albums",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MapY",
                table: "Albums",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectorId",
                table: "Albums",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResidentSectorId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MapX",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "MapY",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "SectorId",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "MapX",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "MapY",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "SectorId",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "MapX",
                table: "Albums");

            migrationBuilder.DropColumn(
                name: "MapY",
                table: "Albums");

            migrationBuilder.DropColumn(
                name: "SectorId",
                table: "Albums");
        }
    }
}
