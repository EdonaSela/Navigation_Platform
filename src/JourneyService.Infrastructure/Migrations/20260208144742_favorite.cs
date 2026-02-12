using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JourneyService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class favorite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FavoritedByUsers",
                table: "Journeys");

            migrationBuilder.CreateTable(
                name: "JourneyFavorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JourneyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JourneyFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JourneyFavorites_Journeys_JourneyId",
                        column: x => x.JourneyId,
                        principalTable: "Journeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JourneyFavorites_JourneyId",
                table: "JourneyFavorites",
                column: "JourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_JourneyFavorites_UserId",
                table: "JourneyFavorites",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JourneyFavorites");

            migrationBuilder.AddColumn<string>(
                name: "FavoritedByUsers",
                table: "Journeys",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
