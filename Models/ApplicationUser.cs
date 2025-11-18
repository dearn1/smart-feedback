using Microsoft.AspNetCore.Identity;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System.ComponentModel.DataAnnotations;

namespace smart_feedback.Models
{
    public class ApplicationUser : IdentityUser
    {

        public string? FullName { get; set; }

        public string? Department { get; set; }

        public string? JobTitle { get; set; }
    }
}
