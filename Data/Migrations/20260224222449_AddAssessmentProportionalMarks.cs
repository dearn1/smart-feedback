using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace smart_feedback.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentProportionalMarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ProportionalMarks",
                table: "Assessments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProportionalMarks",
                table: "Assessments");
        }
    }
}
