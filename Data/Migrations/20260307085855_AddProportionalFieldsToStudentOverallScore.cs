using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace smart_feedback.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProportionalFieldsToStudentOverallScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ProportionalFinalScore",
                table: "StudentOverallScores",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<decimal>(
                name: "ProportionalMarks",
                table: "StudentOverallScores",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProportionalFinalScore",
                table: "StudentOverallScores");

            migrationBuilder.DropColumn(
                name: "ProportionalMarks",
                table: "StudentOverallScores");
        }
    }
}
