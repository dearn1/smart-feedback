namespace smart_feedback.Models
{
    public class RubricDetailsViewModel
    {
        public Rubrics Rubric { get; set; }
        public List<RubricTask> RubricTasks { get; set; } = new List<RubricTask>();
        public List<RubricCriteria> RubricCriterias { get; set;} = new List<RubricCriteria>();
        public List<RubricCriteriaScore> RubricCriteriaScores { get; set;} = new List<RubricCriteriaScore>();
    }
}
