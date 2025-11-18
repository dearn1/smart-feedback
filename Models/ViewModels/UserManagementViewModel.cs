using System.ComponentModel.DataAnnotations;

namespace smart_feedback.Models.ViewModels
{
    public class UserManagementViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string JobTitle { get; set; }
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Display(Name = "Full Name")]
        public string FullName { get; set; }
    }

    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        [Display(Name = "Job Title")]
        public string JobTitle { get; set; }
    }
}
