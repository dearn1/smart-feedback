using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using smart_feedback.Models.ViewModels;

namespace smart_feedback.Services
{
    public class PdfGenerationService : IPdfGenerationService
    {
        private readonly ILogger<PdfGenerationService> _logger;

        public PdfGenerationService(ILogger<PdfGenerationService> logger)
        {
            _logger = logger;
            
            // Set QuestPDF license (Community license is free for open-source and small businesses)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateStudentFeedbackPdf(StudentFeedbackViewModel feedback)
        {
            try
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        // Header
                        page.Header().Element(c => ComposeHeader(c, feedback));

                        // Content
                        page.Content().Element(c => ComposeContent(c, feedback));

                        // Footer
                        page.Footer().Element(c => ComposeFooter(c, feedback));
                    });
                });

                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for student {StudentId}", feedback.Student.StudentId);
                throw;
            }
        }

        public async Task<List<(string FileName, byte[] PdfData)>> GenerateBatchPdfsAsync(List<StudentFeedbackViewModel> feedbacks)
        {
            var pdfFiles = new List<(string FileName, byte[] PdfData)>();

            await Task.Run(() =>
            {
                foreach (var feedback in feedbacks)
                {
                    try
                    {
                        var pdfData = GenerateStudentFeedbackPdf(feedback);
                        var fileName = $"{feedback.Student.StudentId}_{feedback.Student.Name.Replace(" ", "_")}_Feedback.pdf";
                        pdfFiles.Add((fileName, pdfData));

                        _logger.LogInformation("Generated PDF for student {StudentId}", feedback.Student.StudentId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating PDF for student {StudentId}", feedback.Student.StudentId);
                    }
                }
            });

            return pdfFiles;
        }

        private void ComposeHeader(IContainer container, StudentFeedbackViewModel model)
        {
            container.Column(column =>
            {
                // Title section with gradient-like effect
                column.Item().Background(Colors.Blue.Lighten3).Padding(15).Column(titleColumn =>
                {
                    titleColumn.Item().Text("Feedback Report")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    titleColumn.Item().Text(model.Student.Name)
                        .FontSize(18)
                        .SemiBold()
                        .FontColor(Colors.Purple.Darken1);
                });

                column.Item().PaddingVertical(10);

                // Information section
                column.Item().Row(row =>
                {
                    // Left column - Student Info
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Student Information").FontSize(12).Bold().FontColor(Colors.Blue.Darken1);
                        col.Item().PaddingTop(5);
                        col.Item().Text($"Name: {model.Student.Name}").FontSize(10);
                        col.Item().Text($"Student ID: {model.Student.StudentId}").FontSize(10);
                        col.Item().Text($"Email: {model.Student.Email}").FontSize(10);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Assessment Information").FontSize(12).Bold().FontColor(Colors.Blue.Darken1);
                        col.Item().PaddingTop(5);
                        col.Item().Text($"Course: {model.Assessment.CourseCode} - {model.Assessment.Rubric?.CourseName}").FontSize(10);
                        col.Item().Text($"Programme: {model.Assessment.Rubric?.Programme}").FontSize(10);
                        col.Item().Text($"Assignment: {model.Assessment.Rubric?.RubricName}").FontSize(10);
                        col.Item().Text($"Year: {model.Assessment.Year}").FontSize(10);
                        col.Item().Text($"Trimester: {model.Assessment.Trimester}").FontSize(10);
                    });
                });

                column.Item().PaddingVertical(5).LineHorizontal(2).LineColor(Colors.Blue.Lighten2);
            });
        }

        private void ComposeContent(IContainer container, StudentFeedbackViewModel model)
        {
            container.Column(column =>
            {
                // Score Summary
                column.Item().Element(c => ComposeScoreSummary(c, model));

                column.Item().PaddingVertical(10);

                // Task Summaries
                if (model.TaskSummaries != null && model.TaskSummaries.Any())
                {
                    column.Item().Element(c => ComposeTaskSummaries(c, model));
                    column.Item().PaddingVertical(10);
                }

                // Detailed Results
                column.Item().Element(c => ComposeDetailedResults(c, model));

                // Overall Feedback
                if (!string.IsNullOrEmpty(model.OverallFeedback))
                {
                    column.Item().PageBreak();
                    column.Item().Element(c => ComposeOverallFeedback(c, model));
                }
            });
        }

        private void ComposeScoreSummary(IContainer container, StudentFeedbackViewModel model)
        {
            var gradeText = model.Percentage >= 85 ? "Excellent" :
                           model.Percentage >= 70 ? "Good" :
                           model.Percentage >= 50 ? "Satisfactory" : "Needs Improvement";

            var gradeColor = model.Percentage >= 85 ? Colors.Green.Darken1 :
                            model.Percentage >= 70 ? Colors.Blue.Darken1 :
                            model.Percentage >= 50 ? Colors.Orange.Darken1 : Colors.Red.Darken1;

            container.Background(Colors.Grey.Lighten3).Padding(15).Column(column =>
            {
                column.Item().Text("Score Summary").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingVertical(5);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignCenter().Text($"{model.Percentage:F2}%")
                            .FontSize(36)
                            .Bold()
                            .FontColor(Colors.Green.Darken1);
                        col.Item().AlignCenter().Text("Total Score")
                            .FontSize(12)
                            .FontColor(Colors.Grey.Darken1);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignCenter().Text(gradeText)
                            .FontSize(24)
                            .Bold()
                            .FontColor(gradeColor);
                        col.Item().AlignCenter().Text("Grade Category")
                            .FontSize(12)
                            .FontColor(Colors.Grey.Darken1);
                    });
                });
            });
        }

        private void ComposeTaskSummaries(IContainer container, StudentFeedbackViewModel model)
        {
            container.Column(column =>
            {
                column.Item().Text("Task Performance Summary").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingVertical(5);

                foreach (var taskSummary in model.TaskSummaries.OrderBy(t => t.Key))
                {
                    var summary = taskSummary.Value;
                    var performanceColor = summary.Percentage >= 85 ? Colors.Green.Darken1 :
                                          summary.Percentage >= 70 ? Colors.Blue.Darken1 :
                                          summary.Percentage >= 50 ? Colors.Orange.Darken1 : Colors.Red.Darken1;

                    column.Item().PaddingBottom(8).Border(1).BorderColor(performanceColor).Padding(10).Column(taskCol =>
                    {
                        taskCol.Item().Background(performanceColor).Padding(5).Text(summary.TaskTitle)
                            .FontSize(11)
                            .Bold()
                            .FontColor(Colors.White);

                        taskCol.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text($"{summary.ActualMarks:F2} / {summary.MaxMarks}")
                                .FontSize(14)
                                .Bold()
                                .FontColor(performanceColor);
                            row.AutoItem().Text($"{summary.Percentage:F2}%")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);
                        });
                    });
                }
            });
        }

        private void ComposeDetailedResults(IContainer container, StudentFeedbackViewModel model)
        {
            container.Column(column =>
            {
                column.Item().Text("Detailed Results by Task").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingVertical(5);

                var groupedResults = model.CriteriaResults.GroupBy(r => r.TaskTitle).OrderBy(g => g.Key);

                foreach (var taskGroup in groupedResults)
                {
                    column.Item().PaddingBottom(10).Column(taskCol =>
                    {
                        // Task header
                        taskCol.Item().Background(Colors.Blue.Darken1).Padding(8).Row(row =>
                        {
                            row.RelativeItem().Text(taskGroup.Key)
                                .FontSize(12)
                                .Bold()
                                .FontColor(Colors.White);

                            if (model.TaskSummaries.ContainsKey(taskGroup.Key))
                            {
                                var summary = model.TaskSummaries[taskGroup.Key];
                                row.AutoItem().Text($"{summary.ActualMarks:F2} / {summary.MaxMarks} ({summary.Percentage:F2}%)")
                                    .FontSize(10)
                                    .FontColor(Colors.White);
                            }
                        });

                        // Criteria table
                        taskCol.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Criteria
                                columns.RelativeColumn(1); // Score
                                columns.RelativeColumn(1.5f); // Performance Level
                                columns.RelativeColumn(3.5f); // Feedback
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Criteria").FontSize(9).Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Score").FontSize(9).Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Performance").FontSize(9).Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Feedback").FontSize(9).Bold();
                            });

                            // Rows
                            foreach (var result in taskGroup)
                            {
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Column(col =>
                                {
                                    col.Item().Text(result.CriteriaTitle).FontSize(9).Bold();
                                    col.Item().Text($"Weight: {result.Weight:F0}%").FontSize(8).FontColor(Colors.Grey.Darken1);
                                });

                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5)
                                    .Text($"{result.Score} / {result.MaxScore}").FontSize(9);

                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5)
                                    .Text(result.ScoreDescription).FontSize(8);

                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Column(col =>
                                {
                                    col.Item().Text(result.GeneratedFeedback).FontSize(8);

                                    if (!string.IsNullOrEmpty(result.CustomComment))
                                    {
                                        col.Item().PaddingTop(5).Background(Colors.Blue.Lighten4).Padding(5).Column(commentCol =>
                                        {
                                            commentCol.Item().Text("Lecturer Comment:").FontSize(8).Bold().FontColor(Colors.Blue.Darken1);
                                            commentCol.Item().Text(result.CustomComment).FontSize(8).FontColor(Colors.Blue.Darken1);
                                        });
                                    }
                                });
                            }
                        });
                    });
                }
            });
        }

        private void ComposeOverallFeedback(IContainer container, StudentFeedbackViewModel model)
        {
            container.Background(Colors.Grey.Lighten3).Padding(15).Column(column =>
            {
                column.Item().Text("Overall Assessment Feedback").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingVertical(5);

                column.Item().Background(Colors.White).Padding(10).Border(2).BorderColor(Colors.Blue.Lighten2)
                    .Text(model.OverallFeedback)
                    .FontSize(10)
                    .LineHeight(1.5f);
            });
        }

        private void ComposeFooter(IContainer container, StudentFeedbackViewModel model)
        {
            var gradeText = model.Percentage >= 85 ? "Outstanding Performance" :
                           model.Percentage >= 70 ? "Good Performance" :
                           model.Percentage >= 50 ? "Satisfactory Performance" : "Room for Growth";

            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(Colors.Blue.Lighten2);
                column.Item().PaddingVertical(5);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Generated: {DateTime.Now:dd MMM yyyy, hh:mm tt}")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);

                    row.AutoItem().Text($"Grade: {gradeText}")
                        .FontSize(8)
                        .FontColor(Colors.Blue.Darken1);
                });

                column.Item().AlignCenter().Text("Smart Feedback System")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);
            });
        }
    }
}