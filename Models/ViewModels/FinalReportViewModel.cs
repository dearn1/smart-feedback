using smart_feedback.Models;

namespace smart_feedback.Models.ViewModels
{
    public class FinalReportViewModel
    {
        public Student Student { get; set; } = null!;
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Trimester { get; set; }
        public double FinalScore { get; set; }
        public string FinalGrade { get; set; } = string.Empty;
        public string FinalGradeDescription { get; set; } = string.Empty;
        public List<AssessmentScoreBreakdown> AssessmentBreakdown { get; set; } = new();

        // Batch mode properties
        public bool IsBatchMode { get; set; }
        public int CurrentStudentIndex { get; set; }
        public int TotalStudents { get; set; }
        public List<Student> AllStudents { get; set; } = new();
        public List<FinalReportViewModel> AllStudentReports { get; set; } = new();
        public int CourseRolesId { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class AssessmentScoreBreakdown
    {
        public string AssessmentName { get; set; } = string.Empty;
        public decimal ProportionalMarks { get; set; }
        public double TotalActualScore { get; set; }
        public double ProportionalFinalScore { get; set; }
    }
}