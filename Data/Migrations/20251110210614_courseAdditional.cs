using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace smart_feedback.Data.Migrations
{
    /// <inheritdoc />
    public partial class courseAdditional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Institution",
                table: "CourseRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Programme",
                table: "CourseRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Institution",
                table: "CourseRoles");

            migrationBuilder.DropColumn(
                name: "Programme",
                table: "CourseRoles");
        }
    }
}
