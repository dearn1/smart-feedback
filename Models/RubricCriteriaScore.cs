namespace smart_feedback.Models
{
    public class RubricCriteriaScore
    {
        public int RubricCriteriaScoreId { get; set; }
        public int RubricCriteriaId { get; set; }
        public int CriterionScore { get; set; }
        public string ScoreTitle { get; set; }
        public string ScoreDescription { get; set; }

    }
}
