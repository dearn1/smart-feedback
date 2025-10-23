namespace smart_feedback.Models
{
    public class RubricTask
    {
        public int RubricTaskId { get; set; }
        public int RubricsId { get; set; }
        public string TaskTitle { get; set; }
        public string TaskDescription { get; set; }
        public int MaxMarks { get; set; }
    }
}
