using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace smart_feedback.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentScoreTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentOverallScores",
                columns: table => new
                {
                    StudentOverallScoreId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    TotalActualScore = table.Column<double>(type: "float", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentOverallScores", x => x.StudentOverallScoreId);
                    table.ForeignKey(
                        name: "FK_StudentOverallScores_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "AssessmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentOverallScores_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentTaskScores",
                columns: table => new
                {
                    StudentTaskScoreId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    RubricTaskId = table.Column<int>(type: "int", nullable: false),
                    ActualScore = table.Column<double>(type: "float", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentTaskScores", x => x.StudentTaskScoreId);
                    table.ForeignKey(
                        name: "FK_StudentTaskScores_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "AssessmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentTaskScores_RubricTask_RubricTaskId",
                        column: x => x.RubricTaskId,
                        principalTable: "RubricTask",
                        principalColumn: "RubricTaskId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentTaskScores_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentOverallScores_AssessmentId",
                table: "StudentOverallScores",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentOverallScores_StudentId",
                table: "StudentOverallScores",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTaskScores_AssessmentId",
                table: "StudentTaskScores",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTaskScores_RubricTaskId",
                table: "StudentTaskScores",
                column: "RubricTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTaskScores_StudentId",
                table: "StudentTaskScores",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentOverallScores");

            migrationBuilder.DropTable(
                name: "StudentTaskScores");
        }
    }
}
