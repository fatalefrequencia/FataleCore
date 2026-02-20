using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FataleCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCustomization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration effectively skipped because DB is already ahead
            /*
            migrationBuilder.DropForeignKey(
                name: "FK_UserLikes_Tracks_TrackId",
                table: "UserLikes");
            // ... (rest of the migration code)
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Artists_Tracks_FeaturedTrackId",
                table: "Artists");

            migrationBuilder.DropForeignKey(
                name: "FK_UserLikes_Tracks_TrackId",
                table: "UserLikes");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Artists_FeaturedTrackId",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TextColor",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ThemeColor",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "FeaturedTrackId",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "IsLive",
                table: "Artists");

            migrationBuilder.AlterColumn<int>(
                name: "Duration",
                table: "YoutubeTracks",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "TrackId",
                table: "UserLikes",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "YoutubeTrackId",
                table: "UserLikes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLikes_YoutubeTrackId",
                table: "UserLikes",
                column: "YoutubeTrackId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserLikes_Tracks_TrackId",
                table: "UserLikes",
                column: "TrackId",
                principalTable: "Tracks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserLikes_YoutubeTracks_YoutubeTrackId",
                table: "UserLikes",
                column: "YoutubeTrackId",
                principalTable: "YoutubeTracks",
                principalColumn: "Id");
        }
    }
}
