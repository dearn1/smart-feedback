using System.ComponentModel.DataAnnotations;

namespace smart_feedback.Models
{
    public class StudentTaskScore
    {
        [Key]
        public int StudentTaskScoreId { get; set; }
        
        public int AssessmentId { get; set; }
        
        public int StudentId { get; set; }
        
        public int RubricTaskId { get; set; }
        
        /// <summary>
        /// Actual marks achieved for this task
        /// </summary>
        public double ActualScore { get; set; }
        
        public DateTime LastModified { get; set; }
        
        // Navigation properties
        public virtual Assessment Assessment { get; set; }
        public virtual Student Student { get; set; }
        public virtual RubricTask RubricTask { get; set; }
    }
}