using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace smart_feedback.Models
{
    public class Assessment
    {
        public int AssessmentId { get; set; }

        [Required]
        public string AssessmentName { get; set; }
        public string CourseCode { get; set; }
        public int Year { get; set; }
        public int Trimester { get; set; }
        public int RubricsId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Marking";

        public DateTime? StatusChangedDate { get; set; }

        public string StatusChangedBy { get; set; }

        // Helper property for enum conversion
        [NotMapped]
        public AssessmentStatus CurrentStatus
        {
            get => Enum.Parse<AssessmentStatus>(Status);
            set => Status = value.ToString();
        }

        // Navigation properties
        [ForeignKey("RubricsId")]
        public virtual Rubrics Rubric { get; set; }
        public virtual List<StudentAssessmentScore> StudentScores { get; set; } = new();
    }
}
