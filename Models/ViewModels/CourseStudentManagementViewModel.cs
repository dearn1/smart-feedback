using System.Collections.Generic;

namespace smart_feedback.Models.ViewModels
{
    public class CourseStudentManagementViewModel
    {
        public int CourseRolesId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int Year { get; set; }
        public int Trimester { get; set; }
        public string Programme { get; set; }
        public string Role { get; set; }
        
        // All students in the system
        public List<Student> AllStudents { get; set; } = new();
        
        // Students already enrolled in this course
        public List<StudentEnrollmentInfo> EnrolledStudents { get; set; } = new();
        
        // Students not enrolled yet
        public List<Student> AvailableStudents { get; set; } = new();
    }
    
    public class StudentEnrollmentInfo
    {
        public int CourseStudentId { get; set; }
        public int StudentId { get; set; }
        public string StudentIdNumber { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime EnrolledDate { get; set; }
    }
}