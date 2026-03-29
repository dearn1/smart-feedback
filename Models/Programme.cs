using System.ComponentModel.DataAnnotations;

namespace smart_feedback.Models
{
    public class Programme
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Programme")]
        [StringLength(200)]
        public string ProgrammeName { get; set; } = string.Empty;
    }
}