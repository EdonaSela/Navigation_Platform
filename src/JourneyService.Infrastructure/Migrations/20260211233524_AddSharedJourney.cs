using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JourneyService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedJourney : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JourneyShares",
                columns: table => new
                {
                    JourneyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedWithUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JourneyShares", x => new { x.JourneyId, x.SharedWithUserId });
                    table.ForeignKey(
                        name: "FK_JourneyShares_Journeys_JourneyId",
                        column: x => x.JourneyId,
                        principalTable: "Journeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JourneyShares_SharedWithUserId",
                table: "JourneyShares",
                column: "SharedWithUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JourneyShares");
        }
    }
}
