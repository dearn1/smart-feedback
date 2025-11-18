namespace smart_feedback.Models
{
    public class StudentAssessmentScore
    {
        public int StudentAssessmentScoreId { get; set; }
        public int AssessmentId { get; set; }
        public int StudentId { get; set; }
        public int RubricCriteriaId { get; set; }
        public int Score { get; set; } // 0-4
        public string? CustomComment { get; set; }
        public DateTime LastModified { get; set; }

        // Navigation properties
        public virtual Assessment Assessment { get; set; }
        public virtual Student Student { get; set; }
        public virtual RubricCriteria RubricCriteria { get; set; }
    }
}
