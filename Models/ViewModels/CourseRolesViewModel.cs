namespace smart_feedback.Models.ViewModels
{
    public class CourseRolesViewModel
    {
        public int CourseRolesId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string TermName { get; set; }
        public string Programme { get; set; }
        public string Institution { get; set; }
        public string RoleLecturer { get; set; }
        public string RoleModerator { get; set; }
        public string LecturerFullName { get; set; }
        public string ModeratorFullName { get; set; }
    }
}