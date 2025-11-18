namespace smart_feedback.Models
{
    public class Assessment
    {
        public int AssessmentId { get; set; }
        public string AssessmentName { get; set; }
        public string CourseCode { get; set; }
        public string TermName { get; set; }
        public int RubricsId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        
        // Navigation properties
        public virtual Rubrics Rubric { get; set; }
        public virtual List<StudentAssessmentScore> StudentScores { get; set; } = new();
    }
}
