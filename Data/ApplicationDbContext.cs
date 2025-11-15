using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using smart_feedback.Models;

namespace smart_feedback.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<smart_feedback.Models.Student> Student { get; set; } = default!;
        public DbSet<smart_feedback.Models.Rubrics> Rubrics { get; set; } = default!;
        public DbSet<smart_feedback.Models.RubricTask> RubricTask { get; set; } = default!;
        public DbSet<smart_feedback.Models.RubricCriteria> RubricCriteria { get; set;} = default!;
        public DbSet<smart_feedback.Models.RubricCriteriaScore> RubricCriteriaScore { get; set; } = default!;
        public DbSet<smart_feedback.Models.CourseRoles> CourseRoles { get; set; } = default!;
        public DbSet<smart_feedback.Models.Assessment> Assessments { get; set; } = default!;
        public DbSet<smart_feedback.Models.StudentAssessmentScore> StudentAssessmentScores { get; set; } = default!;
    }
}
