using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace smart_feedback.Models.ViewModels
{
    public class AssessmentRubricRow
    {
        public int Index { get; set; }
        
        [Required]
        public string AssessmentName { get; set; }
        
        public int? RubricId { get; set; }
        
        [Required]
        [Range(0, 100, ErrorMessage = "Proportional marks must be between 0 and 100")]
        public decimal ProportionalMarks { get; set; }
    }

    public class AssessmentRubricManagementViewModel
    {
        public int CourseRolesId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int Year { get; set; }
        public int Trimester { get; set; }
        public string Programme { get; set; }
        public int TotalAssessment { get; set; }
        public string UserRole { get; set; }
        
        public List<AssessmentRubricRow> AssessmentRows { get; set; } = new List<AssessmentRubricRow>();
        
        // Available rubrics filtered by programme and course code by default
        public List<Rubrics> AvailableRubrics { get; set; } = new List<Rubrics>();
        
        // All rubrics for when users want to select others
        public List<Rubrics> AllRubrics { get; set; } = new List<Rubrics>();
        
        public decimal TotalProportionalMarks => AssessmentRows.Sum(a => a.ProportionalMarks);
    }
}