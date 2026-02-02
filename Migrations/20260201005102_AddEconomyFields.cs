using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FataleCore.Migrations
{
    /// <inheritdoc />
    public partial class AddEconomyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrackPurchases_Users_UserId",
                table: "TrackPurchases");

            migrationBuilder.DropIndex(
                name: "IX_TrackPurchases_UserId",
                table: "TrackPurchases");

            migrationBuilder.AddColumn<bool>(
                name: "IsDownloadable",
                table: "Tracks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "Tracks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Price",
                table: "Tracks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDownloadable",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Tracks");

            migrationBuilder.CreateIndex(
                name: "IX_TrackPurchases_UserId",
                table: "TrackPurchases",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrackPurchases_Users_UserId",
                table: "TrackPurchases",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
