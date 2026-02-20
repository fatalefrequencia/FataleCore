using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FataleCore.Migrations
{
    /// <inheritdoc />
    public partial class AddYoutubeTracks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserLikes_Tracks_TrackId",
                table: "UserLikes");

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

            migrationBuilder.CreateTable(
                name: "YoutubeTracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    YoutubeId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    ChannelTitle = table.Column<string>(type: "TEXT", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "TEXT", nullable: false),
                    ViewCount = table.Column<long>(type: "INTEGER", nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YoutubeTracks", x => x.Id);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserLikes_Tracks_TrackId",
                table: "UserLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_UserLikes_YoutubeTracks_YoutubeTrackId",
                table: "UserLikes");

            migrationBuilder.DropTable(
                name: "YoutubeTracks");

            migrationBuilder.DropIndex(
                name: "IX_UserLikes_YoutubeTrackId",
                table: "UserLikes");

            migrationBuilder.DropColumn(
                name: "YoutubeTrackId",
                table: "UserLikes");

            migrationBuilder.AlterColumn<int>(
                name: "TrackId",
                table: "UserLikes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserLikes_Tracks_TrackId",
                table: "UserLikes",
                column: "TrackId",
                principalTable: "Tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
