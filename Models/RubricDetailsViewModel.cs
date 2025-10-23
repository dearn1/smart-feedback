namespace smart_feedback.Models
{
    public class RubricDetailsViewModel
    {
        public Rubrics Rubric { get; set; }
        public List<RubricTask> RubricTasks { get; set; } = new List<RubricTask>();
    }
}
