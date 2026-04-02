using System.ComponentModel.DataAnnotations;

namespace smart_feedback.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }
        
        public string TableName { get; set; }
        
        public string Action { get; set; } // INSERT, UPDATE, DELETE
        
        public string KeyValues { get; set; } // Primary key values
        
        public string? OldValues { get; set; } // JSON of old values
        
        public string? NewValues { get; set; } // JSON of new values
        
        public string ChangedBy { get; set; } // Username
        
        public DateTime ChangedAt { get; set; }
    }
}