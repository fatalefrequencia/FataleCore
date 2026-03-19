using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FataleCore.Migrations
{
    /// <inheritdoc />
    public partial class FixMissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix missing columns in Artists table
            migrationBuilder.AddColumn<int>(
                name: "FeaturedTrackId",
                table: "Artists",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLive",
                table: "Artists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CreditsBalance",
                table: "Artists",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Fix missing columns in Users table
            migrationBuilder.AddColumn<string>(
                name: "Biography",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CreditsBalance",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Fix missing columns in Tracks table
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Tracks",
                type: "TEXT",
                nullable: true);

            // Create index for FeaturedTrackId
            migrationBuilder.CreateIndex(
                name: "IX_Artists_FeaturedTrackId",
                table: "Artists",
                column: "FeaturedTrackId");

            // Add foreign key for FeaturedTrackId
            migrationBuilder.AddForeignKey(
                name: "FK_Artists_Tracks_FeaturedTrackId",
                table: "Artists",
                column: "FeaturedTrackId",
                principalTable: "Tracks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Artists_Tracks_FeaturedTrackId",
                table: "Artists");

            migrationBuilder.DropIndex(
                name: "IX_Artists_FeaturedTrackId",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "FeaturedTrackId",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "IsLive",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "CreditsBalance",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "Biography",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreditsBalance",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Tracks");
        }
    }
}
