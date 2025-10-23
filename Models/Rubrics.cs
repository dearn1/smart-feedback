namespace smart_feedback.Models
{
    public class Rubrics
    {
        
        public int RubricsId { get; set; }
        public string RubricName { get; set; }
        public string Institution { get; set; }
        public string Programme { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int TotalMarks { get; set; }
        public string SourceFile { get; set; }
    }
}
