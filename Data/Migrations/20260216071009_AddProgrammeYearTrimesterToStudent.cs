using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace smart_feedback.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProgrammeYearTrimesterToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Programme",
                table: "Student",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TrimesterEnrolled",
                table: "Student",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "YearEnrolled",
                table: "Student",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Programme",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "TrimesterEnrolled",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "YearEnrolled",
                table: "Student");
        }
    }
}
