
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ThptA.Models
{
    [Index(
        nameof(StudentId),
        nameof(AcademicYearId),
        nameof(Title),
        IsUnique = true)]
    public class StudentFee
    {
        public int Id { get; set; }

        // Học sinh phải đóng khoản tiền này
        public int StudentId { get; set; }

        public Student Student { get; set; } = null!;

        // Năm học
        public int AcademicYearId { get; set; }

        public AcademicYear AcademicYear { get; set; }
            = null!;

        // Tên khoản thu
        [Required]
        [StringLength(120)]
        public string Title { get; set; } = "";

        // Tổng tiền phải đóng
        [Precision(18, 2)]
        [Range(typeof(decimal), "0.01", "999999999999.99")]
        public decimal AmountDue { get; set; }

        // Hạn đóng tiền
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        public DateTime CreatedAtUtc { get; set; }
            = DateTime.UtcNow;

        // Các lần đóng tiền
        public List<FeePayment> Payments { get; set; }
            = new();
    }
}
