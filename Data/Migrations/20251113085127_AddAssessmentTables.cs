using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace smart_feedback.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    AssessmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CourseCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TermName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RubricsId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.AssessmentId);
                    table.ForeignKey(
                        name: "FK_Assessments_Rubrics_RubricsId",
                        column: x => x.RubricsId,
                        principalTable: "Rubrics",
                        principalColumn: "RubricsId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentAssessmentScores",
                columns: table => new
                {
                    StudentAssessmentScoreId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    RubricCriteriaId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    CustomComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAssessmentScores", x => x.StudentAssessmentScoreId);
                    table.ForeignKey(
                        name: "FK_StudentAssessmentScores_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "AssessmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentAssessmentScores_RubricCriteria_RubricCriteriaId",
                        column: x => x.RubricCriteriaId,
                        principalTable: "RubricCriteria",
                        principalColumn: "RubricCriteriaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentAssessmentScores_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_RubricsId",
                table: "Assessments",
                column: "RubricsId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssessmentScores_AssessmentId",
                table: "StudentAssessmentScores",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssessmentScores_RubricCriteriaId",
                table: "StudentAssessmentScores",
                column: "RubricCriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssessmentScores_StudentId",
                table: "StudentAssessmentScores",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentAssessmentScores");

            migrationBuilder.DropTable(
                name: "Assessments");
        }
    }
}
