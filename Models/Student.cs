using Microsoft.EntityFrameworkCore; // Added this namespace for the Index attribute  


namespace smart_feedback.Models
{
    [Index(nameof(StudentId), IsUnique = true)] // Moved Index attribute to class level  
    public class Student
    {
        public int Id { get; set; }

        public string StudentId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Programme { get; set; }
        public int YearEnrolled { get; set; }
        public int TrimesterEnrolled { get; set; }
    }
}
