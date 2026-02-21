using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FataleCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentThreadingv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "FeedInteractions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeedInteractions_ParentId",
                table: "FeedInteractions",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_FeedInteractions_FeedInteractions_ParentId",
                table: "FeedInteractions",
                column: "ParentId",
                principalTable: "FeedInteractions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeedInteractions_FeedInteractions_ParentId",
                table: "FeedInteractions");

            migrationBuilder.DropIndex(
                name: "IX_FeedInteractions_ParentId",
                table: "FeedInteractions");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "FeedInteractions");
        }
    }
}
