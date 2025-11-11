using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace smart_feedback.Data.Migrations
{
    /// <inheritdoc />
    public partial class addScoreTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScoreTitle",
                table: "RubricCriteriaScore",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScoreTitle",
                table: "RubricCriteriaScore");
        }
    }
}
