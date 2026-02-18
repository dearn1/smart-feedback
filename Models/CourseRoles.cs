namespace smart_feedback.Models
{
    public class CourseRoles
    {
        public int CourseRolesId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int Year { get; set; }
        public int Trimester { get; set; }
        public string Programme { get; set; }
        public string Institution { get; set; }
        public string RoleLecturer { get; set; }
        public string RoleModerator { get; set; }
        public int TotalAssessment { get; set; }
        public string Status { get; set; } = "Active"; // Default to Active
    }
}
