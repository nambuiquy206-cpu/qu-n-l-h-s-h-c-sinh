
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ThptA.Models
{
    public class AppUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }
}
