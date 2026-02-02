namespace smart_feedback.Models
{
    public class CourseStudent
    {
        public int CourseStudentId { get; set; }
        public int CourseRolesId { get; set; }
        public int StudentId { get; set; }
        public DateTime EnrolledDate { get; set; }
        
        // Navigation properties
        public virtual CourseRoles CourseRoles { get; set; }
        public virtual Student Student { get; set; }
    }
}