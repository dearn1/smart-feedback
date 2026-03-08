using smart_feedback.Models;

namespace smart_feedback.Models.ViewModels
{
    public class FinalReportViewModel
    {
        public Student Student { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int Year { get; set; }
        public int Trimester { get; set; }
        
        // Final Score Information
        public double FinalScore { get; set; }
        public string FinalGrade { get; set; }
        public string FinalGradeDescription { get; set; }
        
        // Assessment Breakdown
        public List<AssessmentScoreBreakdown> AssessmentBreakdown { get; set; } = new();
        
        // For batch mode
        public bool IsBatchMode { get; set; }
        public int CurrentStudentIndex { get; set; }
        public int TotalStudents { get; set; }
        public List<Student> AllStudents { get; set; } = new();
        public List<FinalReportViewModel> AllStudentReports { get; set; } = new();
        
        // For navigation
        public int CourseRolesId { get; set; }
        public string Role { get; set; }
    }
    
    public class AssessmentScoreBreakdown
    {
        public string AssessmentName { get; set; }
        public decimal ProportionalMarks { get; set; }
        public double TotalActualScore { get; set; }
        public double ProportionalFinalScore { get; set; }
    }
}