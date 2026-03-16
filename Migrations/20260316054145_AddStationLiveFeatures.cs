using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FataleCore.Migrations
{
    /// <inheritdoc />
    public partial class AddStationLiveFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunityFollows");

            migrationBuilder.AddColumn<bool>(
                name: "IsChatEnabled",
                table: "Stations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsQueueEnabled",
                table: "Stations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsChatEnabled",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "IsQueueEnabled",
                table: "Stations");

            migrationBuilder.CreateTable(
                name: "CommunityFollows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommunityId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_CommunityFollows_CommunityId",
                table: "CommunityFollows",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityFollows_UserId_CommunityId",
                table: "CommunityFollows",
                columns: new[] { "UserId", "CommunityId" },
                unique: true);
        }
    }
}
