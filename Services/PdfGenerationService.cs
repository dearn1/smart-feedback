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

        public async Task<List<(string fileName, byte[] pdfData)>> GenerateFinalReportPdfsAsync(List<FinalReportViewModel> reports)
        {
            var pdfFiles = new List<(string fileName, byte[] pdfData)>();
            
            await Task.Run(() =>
            {
                foreach (var report in reports)
                {
                    try
                    {
                        // Generate PDF using QuestPDF
                        var pdfData = GenerateFinalReportPdf(report);

                        // Create filename
                        var fileName = $"FinalReport_{report.Student.StudentId}_{report.Student.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf";
                        
                        // Sanitize filename
                        fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

                        pdfFiles.Add((fileName, pdfData));

                        _logger.LogDebug("Generated Final Report PDF for student {StudentId}", report.Student.StudentId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating Final Report PDF for student {StudentId}", report.Student?.StudentId);
                        // Continue with other students even if one fails
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

        private async Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent)
        {
            // Implementation for converting HTML to PDF using QuestPDF or any other library
            // For example purposes, this method is left blank
            await Task.CompletedTask;
            return Array.Empty<byte>();
        }

        private byte[] GenerateFinalReportPdf(FinalReportViewModel report)
        {
            try
            {
                var gradeColor = report.FinalGradeDescription switch
                {
                    "Excellent" => Colors.Green.Darken1,
                    "Good" => Colors.Blue.Darken1,
                    "Pass" => Colors.Orange.Darken1,
                    _ => Colors.Red.Darken1
                };

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        // Header
                        page.Header().Element(c => ComposeFinalReportHeader(c, report));

                        // Content
                        page.Content().Element(c => ComposeFinalReportContent(c, report, gradeColor));

                        // Footer
                        page.Footer().Element(c => ComposeFinalReportFooter(c));
                    });
                });

                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Final Report PDF for student {StudentId}", report.Student.StudentId);
                throw;
            }
        }

        private void ComposeFinalReportHeader(IContainer container, FinalReportViewModel report)
        {
            container.Column(column =>
            {
                // Title section
                column.Item().Background(Colors.Purple.Darken1).Padding(20).Column(titleColumn =>
                {
                    titleColumn.Item().AlignCenter().Text("🏆 FINAL COURSE REPORT")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.White);

                    titleColumn.Item().AlignCenter().Text($"{report.CourseCode} - {report.CourseName}")
                        .FontSize(14)
                        .FontColor(Colors.White);

                    titleColumn.Item().AlignCenter().Text($"Trimester {report.Trimester}, {report.Year}")
                        .FontSize(12)
                        .FontColor(Colors.White);
                });

                column.Item().PaddingVertical(15);

                // Information section
                column.Item().Row(row =>
                {
                    // Student Info
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Student Information").FontSize(12).Bold().FontColor(Colors.Purple.Darken1);
                        col.Item().PaddingTop(5);
                        col.Item().Text($"Name: {report.Student.Name}").FontSize(10);
                        col.Item().Text($"Student ID: {report.Student.StudentId}").FontSize(10);
                        col.Item().Text($"Email: {report.Student.Email}").FontSize(10);
                    });

                    // Course Info
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Course Information").FontSize(12).Bold().FontColor(Colors.Purple.Darken1);
                        col.Item().PaddingTop(5);
                        col.Item().Text($"Course: {report.CourseCode}").FontSize(10);
                        col.Item().Text($"Course Name: {report.CourseName}").FontSize(10);
                        col.Item().Text($"Period: T{report.Trimester} {report.Year}").FontSize(10);
                    });
                });

                column.Item().PaddingVertical(5).LineHorizontal(2).LineColor(Colors.Purple.Lighten2);
            });
        }

        private void ComposeFinalReportContent(IContainer container, FinalReportViewModel report, string gradeColor)
        {
            container.Column(column =>
            {
                // Score Summary
                column.Item().Background(Colors.Grey.Lighten3).Padding(20).Column(summaryCol =>
                {
                    summaryCol.Item().AlignCenter().Text("Final Score Summary")
                        .FontSize(16)
                        .Bold()
                        .FontColor(Colors.Purple.Darken1);

                    summaryCol.Item().PaddingVertical(10);

                    summaryCol.Item().Row(row =>
                    {
                        // Final Score
                        row.RelativeItem().AlignCenter().Column(col =>
                        {
                            col.Item().Text("Final Score")
                                .FontSize(11)
                                .FontColor(Colors.Grey.Darken1);
                            col.Item().Text($"{report.FinalScore:F2}")
                                .FontSize(36)
                                .Bold()
                                .FontColor(Colors.Blue.Darken1);
                        });

                        // Final Grade
                        row.RelativeItem().AlignCenter().Column(col =>
                        {
                            col.Item().Text("Final Grade")
                                .FontSize(11)
                                .FontColor(Colors.Grey.Darken1);
                            col.Item().Text(report.FinalGrade)
                                .FontSize(36)
                                .Bold()
                                .FontColor(gradeColor);
                        });

                        // Performance
                        row.RelativeItem().AlignCenter().Column(col =>
                        {
                            col.Item().Text("Performance")
                                .FontSize(11)
                                .FontColor(Colors.Grey.Darken1);
                            col.Item().Text(report.FinalGradeDescription)
                                .FontSize(20)
                                .Bold()
                                .FontColor(gradeColor);
                        });
                    });
                });

                column.Item().PaddingVertical(15);

                // Assessment Breakdown Table
                column.Item().Text("Assessment Breakdown").FontSize(14).Bold().FontColor(Colors.Purple.Darken1);
                column.Item().PaddingVertical(5);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3); // Assessment Name
                        columns.RelativeColumn(1); // Total Score
                        columns.RelativeColumn(1); // Weight
                        columns.RelativeColumn(1.5f); // Weighted Score
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Purple.Darken1).Padding(8).Text("Assessment Name").FontSize(10).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Purple.Darken1).Padding(8).AlignCenter().Text("Total Score").FontSize(10).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Purple.Darken1).Padding(8).AlignCenter().Text("Weight (%)").FontSize(10).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Purple.Darken1).Padding(8).AlignCenter().Text("Weighted Score").FontSize(10).Bold().FontColor(Colors.White);
                    });

                    // Rows
                    foreach (var assessment in report.AssessmentBreakdown)
                    {
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Text(assessment.AssessmentName).FontSize(10).Bold();
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).AlignCenter().Text($"{assessment.TotalActualScore:F2}").FontSize(10);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).AlignCenter().Text($"{assessment.ProportionalMarks}%").FontSize(10);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).AlignCenter().Text($"{assessment.ProportionalFinalScore:F2}").FontSize(10).Bold().FontColor(Colors.Green.Darken1);
                    }

                    // Footer Total
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(8).AlignRight().Text("Total Final Score:").FontSize(11).Bold();
                    table.Cell().Background(Colors.Grey.Lighten2).Padding(8).AlignCenter().Text($"{report.FinalScore:F2}").FontSize(14).Bold().FontColor(Colors.Purple.Darken1);

                });
            });
        }

        private void ComposeFinalReportFooter(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                column.Item().PaddingVertical(8);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Report Generated: {DateTime.Now:dd MMMM yyyy, hh:mm tt}")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);

                    row.AutoItem().Text($"Smart Assessment Feedback System © {DateTime.Now.Year}")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);
                });
            });
        }

        private string GenerateFinalReportHtml(FinalReportViewModel report)
        {
            var gradeColor = report.FinalGradeDescription switch
            {
                "Excellent" => "#28a745",
                "Good" => "#007bff",
                "Pass" => "#ffc107",
                _ => "#dc3545"
            };

            var gradeIcon = report.FinalGradeDescription switch
            {
                "Excellent" => "🏆",
                "Good" => "👍",
                "Pass" => "✓",
                _ => "✗"
            };

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            font-size: 11pt;
            line-height: 1.6;
            color: #333;
            padding: 20px;
        }}
        
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            border-radius: 10px;
            margin-bottom: 30px;
            text-align: center;
        }}
        
        .header h1 {{
            font-size: 28pt;
            margin-bottom: 10px;
        }}
        
        .header p {{
            font-size: 12pt;
            opacity: 0.9;
        }}
        
        .info-section {{
            display: table;
            width: 100%;
            margin-bottom: 30px;
        }}
        
        .info-column {{
            display: table-cell;
            width: 50%;
            padding: 15px;
            vertical-align: top;
        }}
        
        .info-column h3 {{
            color: #667eea;
            border-bottom: 2px solid #667eea;
            padding-bottom: 5px;
            margin-bottom: 15px;
        }}
        
        .info-item {{
            margin-bottom: 10px;
        }}
        
        .info-item strong {{
            color: #555;
            display: inline-block;
            width: 120px;
        }}
        
        .score-summary {{
            background: linear-gradient(135deg, #f8f9fa 0%, #ffffff 100%);
            border: 3px solid #667eea;
            border-radius: 10px;
            padding: 30px;
            margin-bottom: 30px;
            text-align: center;
        }}
        
        .score-grid {{
            display: table;
            width: 100%;
            margin-top: 20px;
        }}
        
        .score-cell {{
            display: table-cell;
            width: 33.33%;
            padding: 20px;
            text-align: center;
            border-right: 1px solid #dee2e6;
        }}
        
        .score-cell:last-child {{
            border-right: none;
        }}
        
        .score-value {{
            font-size: 48pt;
            font-weight: bold;
            margin: 10px 0;
        }}
        
        .score-label {{
            color: #666;
            font-size: 12pt;
            text-transform: uppercase;
            letter-spacing: 1px;
        }}
        
        .grade-value {{
            color: {gradeColor};
        }}
        
        .assessment-table {{
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 30px;
        }}
        
        .assessment-table th {{
            background: #667eea;
            color: white;
            padding: 12px;
            text-align: left;
            font-weight: 600;
        }}
        
        .assessment-table td {{
            padding: 12px;
            border: 1px solid #dee2e6;
        }}
        
        .assessment-table tbody tr:nth-child(even) {{
            background-color: #f8f9fa;
        }}
        
        .assessment-table tfoot {{
            background: #e9ecef;
            font-weight: bold;
        }}
        
        .badge {{
            display: inline-block;
            padding: 5px 10px;
            border-radius: 4px;
            font-size: 10pt;
            font-weight: 600;
        }}
        
        .badge-info {{
            background: #17a2b8;
            color: white;
        }}
        
        .badge-success {{
            background: #28a745;
            color: white;
        }}
        
        .badge-primary {{
            background: #007bff;
            color: white;
        }}
        
        .text-center {{
            text-align: center;
        }}
        
        .text-right {{
            text-align: right;
        }}
        
        .footer {{
            margin-top: 40px;
            padding-top: 20px;
            border-top: 2px solid #dee2e6;
            text-align: center;
            color: #666;
            font-size: 9pt;
        }}
        
        h2 {{
            color: #667eea;
            margin-bottom: 20px;
            font-size: 16pt;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>{gradeIcon} Final Course Report</h1>
        <p>{report.CourseCode} - {report.CourseName}</p>
        <p>Trimester {report.Trimester}, {report.Year}</p>
    </div>
    
    <div class='info-section'>
        <div class='info-column'>
            <h3>Student Information</h3>
            <div class='info-item'><strong>Name:</strong> {report.Student.Name}</div>
            <div class='info-item'><strong>Student ID:</strong> {report.Student.StudentId}</div>
            <div class='info-item'><strong>Email:</strong> {report.Student.Email}</div>
        </div>
        <div class='info-column'>
            <h3>Course Information</h3>
            <div class='info-item'><strong>Course:</strong> {report.CourseCode}</div>
            <div class='info-item'><strong>Course Name:</strong> {report.CourseName}</div>
            <div class='info-item'><strong>Period:</strong> T{report.Trimester} {report.Year}</div>
        </div>
    </div>
    
    <div class='score-summary'>
        <h2 style='color: #667eea; margin-bottom: 20px;'>Final Score Summary</h2>
        <div class='score-grid'>
            <div class='score-cell'>
                <div class='score-label'>Final Score</div>
                <div class='score-value' style='color: #007bff;'>{report.FinalScore:F2}</div>
            </div>
            <div class='score-cell'>
                <div class='score-label'>Final Grade</div>
                <div class='score-value grade-value'>{report.FinalGrade}</div>
            </div>
            <div class='score-cell'>
                <div class='score-label'>Performance</div>
                <div class='score-value' style='font-size: 32pt; color: {gradeColor};'>{gradeIcon}</div>
                <div style='color: {gradeColor}; font-weight: bold; margin-top: 10px;'>{report.FinalGradeDescription}</div>
            </div>
        </div>
    </div>
    
    <h2>Assessment Breakdown</h2>
    <table class='assessment-table'>
        <thead>
            <tr>
                <th>Assessment Name</th>
                <th class='text-center'>Total Score</th>
                <th class='text-center'>Weight (%)</th>
                <th class='text-center'>Weighted Score</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var assessment in report.AssessmentBreakdown)
            {
                html += $@"
            <tr>
                <td><strong>{assessment.AssessmentName}</strong></td>
                <td class='text-center'>
                    <span class='badge badge-info'>{assessment.TotalActualScore:F2}</span>
                </td>
                <td class='text-center'>{assessment.ProportionalMarks}%</td>
                <td class='text-center'>
                    <span class='badge badge-success'>{assessment.ProportionalFinalScore:F2}</span>
                </td>
            </tr>";
            }

            html += $@"
        </tbody>
        <tfoot>
            <tr>
                <td class='text-right' colspan='3'><strong>Total Final Score:</strong></td>
                <td class='text-center'>
                    <span class='badge badge-primary' style='font-size: 12pt; padding: 8px 15px;'>{report.FinalScore:F2}</span>
                </td>
            </tr>
        </tfoot>
    </table>
    
    <div class='footer'>
        <p><strong>Report Generated:</strong> {DateTime.Now:dd MMMM yyyy, hh:mm tt}</p>
        <p>Smart Assessment Feedback System © {DateTime.Now.Year}</p>
    </div>
</body>
</html>";

            return html;
        }
    }
}