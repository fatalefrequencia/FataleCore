using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FataleCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityFollow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Stations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommunityFollows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CommunityId = table.Column<int>(type: "INTEGER", nullable: false),
                    FollowedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityFollows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityFollows_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalTable: "Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunityFollows_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommunityMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommunityId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 280, nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityMessages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackFingerprints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrackType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TrackId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    ViewTier = table.Column<int>(type: "INTEGER", nullable: false),
                    ChannelType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EnrichedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlayCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackFingerprints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserListeningEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    TrackType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TrackId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    TrackTitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    ListenedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserListeningEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityFollows_CommunityId",
                table: "CommunityFollows",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityFollows_UserId_CommunityId",
                table: "CommunityFollows",
                columns: new[] { "UserId", "CommunityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityMessages_UserId",
                table: "CommunityMessages",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunityFollows");

            migrationBuilder.DropTable(
                name: "CommunityMessages");

            migrationBuilder.DropTable(
                name: "TrackFingerprints");

            migrationBuilder.DropTable(
                name: "UserListeningEvents");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Stations");
        }
    }
}
