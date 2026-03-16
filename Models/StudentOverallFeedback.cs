using System.ComponentModel.DataAnnotations;

namespace smart_feedback.Models
{
    public class StudentOverallFeedback
    {
        [Key]
        public int StudentOverallFeedbackId { get; set; }
        
        [Required]
        public int AssessmentId { get; set; }
        
        [Required]
        public int StudentId { get; set; }
        
        [Required]
        public string OverallFeedback { get; set; }
        
        public DateTime GeneratedDate { get; set; }
        
        public DateTime? LastModified { get; set; }
        
        // Navigation properties
        public virtual Assessment Assessment { get; set; }
        public virtual Student Student { get; set; }
    }
}