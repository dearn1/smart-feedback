using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using smart_feedback.Models;

namespace smart_feedback.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<smart_feedback.Models.Student> Student { get; set; } = default!;
        public DbSet<smart_feedback.Models.Rubrics> Rubrics { get; set; } = default!;
        public DbSet<smart_feedback.Models.RubricTask> RubricTask { get; set; } = default!;
        public DbSet<smart_feedback.Models.RubricCriteria> RubricCriteria { get; set; } = default!;
        public DbSet<smart_feedback.Models.RubricCriteriaScore> RubricCriteriaScore { get; set; } = default!;
        public DbSet<smart_feedback.Models.CourseRoles> CourseRoles { get; set; } = default!;
        public DbSet<smart_feedback.Models.Assessment> Assessments { get; set; } = default!;
        public DbSet<smart_feedback.Models.StudentAssessmentScore> StudentAssessmentScores { get; set; } = default!;
        public DbSet<smart_feedback.Models.CourseStudent> CourseStudent { get; set; } = default!;
        public DbSet<smart_feedback.Models.StudentOverallFeedback> StudentOverallFeedback { get; set; } = default!;
        public DbSet<smart_feedback.Models.StudentTaskScore> StudentTaskScores { get; set; } = default!;
        public DbSet<smart_feedback.Models.StudentOverallScore> StudentOverallScores { get; set; } = default!;
        public DbSet<smart_feedback.Models.Course> Courses { get; set; } = default!;
        public DbSet<smart_feedback.Models.Programme> Programmes { get; set; } = default!;
        public DbSet<smart_feedback.Models.AuditLog> AuditLogs { get; set; } = default!;

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditEntries = OnBeforeSaveChanges();
            var result = await base.SaveChangesAsync(cancellationToken);
            await OnAfterSaveChanges(auditEntries);
            return result;
        }

        private List<AuditEntry> OnBeforeSaveChanges()
        {
            var auditEntries = new List<AuditEntry>();

            var currentUser = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";

            // Force EF to detect all changes first
            ChangeTracker.DetectChanges();

            foreach (var entry in ChangeTracker.Entries())
            {
                // Skip audit logs themselves and temp data
                if (entry.Entity is AuditLog ||
                    entry.State == EntityState.Detached ||
                    entry.State == EntityState.Unchanged)
                    continue;

                // Skip Identity tables (optional - uncomment if you don't want to audit Identity tables)
                var entityType = entry.Entity.GetType().Name;
                if (entityType.StartsWith("Identity") ||
                    //entityType == "ApplicationUser" ||
                    entityType.Contains("Role") ||
                    entityType.Contains("Claim") ||
                    entityType.Contains("Login") ||
                    entityType.Contains("Token"))
                    continue;

                var auditEntry = new AuditEntry(entry)
                {
                    TableName = entry.Entity.GetType().Name,
                    ChangedBy = currentUser,
                    ChangedAt = DateTime.Now
                };

                // Determine action based on state
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.Action = "INSERT";
                        break;
                    case EntityState.Modified:
                        auditEntry.Action = "UPDATE";
                        break;
                    case EntityState.Deleted:
                        auditEntry.Action = "DELETE";
                        break;
                    default:
                        continue;
                }

                auditEntries.Add(auditEntry);

                // Get database values for modified entities to ensure we have correct original values
                PropertyValues databaseValues = null;
                if (entry.State == EntityState.Modified)
                {
                    try
                    {
                        databaseValues = entry.GetDatabaseValues();
                    }
                    catch
                    {
                        // Entity might not exist in database yet, use OriginalValues
                        databaseValues = null;
                    }
                }

                foreach (var property in entry.Properties)
                {
                    string propertyName = property.Metadata.Name;

                    if (propertyName.Equals("ConcurrencyStamp")) { continue; }

                    if (property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                            break;

                        case EntityState.Deleted:
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            break;

                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                // Use database values if available, otherwise use OriginalValue
                                var originalValue = databaseValues?[propertyName] ?? property.OriginalValue;
                                var currentValue = property.CurrentValue;

                                // Ensure values are actually different
                                if (!Equals(originalValue, currentValue))
                                {
                                    auditEntry.OldValues[propertyName] = originalValue;
                                    auditEntry.NewValues[propertyName] = currentValue;
                                }
                            }
                            break;
                    }
                }
            }

            return auditEntries;
        }

        private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0)
                return;

            foreach (var auditEntry in auditEntries)
            {
                // Only add audit entries that actually have changes
                if (auditEntry.Action == "INSERT" || auditEntry.Action == "DELETE" ||
                    (auditEntry.Action == "UPDATE" && (auditEntry.OldValues.Any() || auditEntry.NewValues.Any())))
                {
                    AuditLogs.Add(auditEntry.ToAuditLog());
                }
            }

            await base.SaveChangesAsync();
        }
    }

    public class AuditEntry
    {
        public AuditEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            Entry = entry;
        }

        public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; }
        public string TableName { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> KeyValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> OldValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> NewValues { get; } = new Dictionary<string, object>();
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }

        public AuditLog ToAuditLog()
        {
            return new AuditLog
            {
                TableName = TableName,
                Action = Action,
                KeyValues = System.Text.Json.JsonSerializer.Serialize(KeyValues),
                OldValues = OldValues.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(OldValues),
                NewValues = NewValues.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(NewValues),
                ChangedBy = ChangedBy,
                ChangedAt = ChangedAt
            };
        }
    }
}