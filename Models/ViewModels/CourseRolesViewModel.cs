namespace smart_feedback.Models.ViewModels
{
    public class CourseRolesViewModel
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
        public string LecturerFullName { get; set; }
        public string ModeratorFullName { get; set; }
        
        // NEW: Assessment status counts
        public int FinalReviewCount { get; set; }
        public int ModerationCount { get; set; }
        public bool HasFinalReview => FinalReviewCount > 0;
        public bool HasModeration => ModerationCount > 0;
    }
}