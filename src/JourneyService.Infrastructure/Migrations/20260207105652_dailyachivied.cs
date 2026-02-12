using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JourneyService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class dailyachivied : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDailyGoalAchieved",
                table: "Journeys",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDailyGoalAchieved",
                table: "Journeys");
        }
    }
}
