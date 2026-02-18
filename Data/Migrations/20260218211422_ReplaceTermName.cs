using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace smart_feedback.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTermName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TermName",
                table: "Rubrics");

            migrationBuilder.DropColumn(
                name: "TermName",
                table: "CourseRoles");

            migrationBuilder.DropColumn(
                name: "TermName",
                table: "Assessments");

            migrationBuilder.AddColumn<int>(
                name: "Trimester",
                table: "Rubrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Rubrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Trimester",
                table: "CourseRoles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "CourseRoles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Trimester",
                table: "Assessments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Assessments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Trimester",
                table: "Rubrics");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Rubrics");

            migrationBuilder.DropColumn(
                name: "Trimester",
                table: "CourseRoles");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "CourseRoles");

            migrationBuilder.DropColumn(
                name: "Trimester",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Assessments");

            migrationBuilder.AddColumn<string>(
                name: "TermName",
                table: "Rubrics",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TermName",
                table: "CourseRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TermName",
                table: "Assessments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
