using System.ComponentModel.DataAnnotations;

namespace smart_feedback.Models
{
    public class RubricCriteria
    {
        public int RubricCriteriaId { get; set; }
        public int RubricTaskId { get; set; }
        public string CriterionTitle { get; set; }
        public double Weight { get; set; }

        [Range(1, 10, ErrorMessage = "Max Score must be between 1 and 10")]
        [Required(ErrorMessage = "Max Score is required")]
        public int MaxScore { get; set; }
    }
}
