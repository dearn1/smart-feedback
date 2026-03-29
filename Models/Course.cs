using System.ComponentModel.DataAnnotations;

namespace smart_feedback.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Course Code")]
        [StringLength(20)]
        public string CourseCode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Course Name")]
        public string CourseName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Programme { get; set; } = string.Empty;
    }
}