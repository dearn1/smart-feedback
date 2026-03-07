using System.ComponentModel.DataAnnotations;

namespace smart_feedback.Models
{
    public class StudentOverallScore
    {
        [Key]
        public int StudentOverallScoreId { get; set; }
        
        public int AssessmentId { get; set; }
        
        public int StudentId { get; set; }
        
        /// <summary>
        /// Total actual score achieved across all tasks
        /// </summary>
        public double TotalActualScore { get; set; }
        
        /// <summary>
        /// Proportional marks from Assessment table (e.g., 100, 50, 25)
        /// </summary>
        public decimal ProportionalMarks { get; set; }
        
        /// <summary>
        /// Final score calculated as: ProportionalMarks / 100.0 * TotalActualScore
        /// </summary>
        public double ProportionalFinalScore { get; set; }
        
        public DateTime LastModified { get; set; }
        
        // Navigation properties
        public virtual Assessment Assessment { get; set; }
        public virtual Student Student { get; set; }
    }
}