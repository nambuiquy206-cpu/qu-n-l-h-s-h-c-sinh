
using System.ComponentModel.DataAnnotations;

namespace ThptA.Models
{
    // 1. Bảng năm học
    public class AcademicYear
    {
        public int Id { get; set; }

        [Required]
        [StringLength(9)]
        public string Code { get; set; } = "";

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }

    // 2. Bảng lớp học
    public class SchoolClass
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Name { get; set; } = "";

        public int Grade { get; set; }

        public int AcademicYearId { get; set; }

        public AcademicYear? AcademicYear { get; set; }

        // Giáo viên được phân công chủ nhiệm
        public string? TeacherId { get; set; }

        public AppUser? Teacher { get; set; }

        // Kiểm soát cập nhật đồng thời
        [Timestamp]
        public byte[] RowVersion { get; set; }
            = Array.Empty<byte>();

        public ICollection<Student> Students { get; set; }
            = new List<Student>();
    }

    // 3. Bảng hồ sơ học sinh
    public class Student
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        public DateTime BirthDate { get; set; }

        [Required]
        [StringLength(1)]
        public string Gender { get; set; } = "M";

        [Required]
        [StringLength(250)]
        public string Address { get; set; } = "";

        public int ClassId { get; set; }

        public SchoolClass? SchoolClass { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime UpdatedAtUtc { get; set; }
            = DateTime.UtcNow;

        [Timestamp]
        public byte[] RowVersion { get; set; }
            = Array.Empty<byte>();

        public PrimaryContact? PrimaryContact { get; set; }
    }

    // 4. Bảng người liên hệ chính
    public class PrimaryContact
    {
        [Key]
        public int StudentId { get; set; }

        public Student? Student { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [Required]
        [StringLength(30)]
        public string Relationship { get; set; } = "";

        [Required]
        [StringLength(16)]
        public string Phone { get; set; } = "";
    }

    // 5. Bảng nhật ký thao tác
    public class AuditLog
    {
        public long Id { get; set; }

        [Required]
        public string ActorId { get; set; } = "";

        public AppUser? Actor { get; set; }

        [Required]
        [StringLength(40)]
        public string Action { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string Target { get; set; } = "";

        public DateTime AtUtc { get; set; }
            = DateTime.UtcNow;
    }
}

