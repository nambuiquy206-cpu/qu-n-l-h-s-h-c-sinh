
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

        // Liên kết tài khoản với hồ sơ học sinh
        // Admin và Teacher sẽ để StudentId = null
        public int? StudentId { get; set; }

        public Student? Student { get; set; }
    }

}
