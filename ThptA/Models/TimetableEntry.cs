
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ThptA.Models
{
    // Không cho trùng tiết trong cùng lớp, cùng ngày
    [Index(
        nameof(SchoolClassId),
        nameof(DayOfWeek),
        nameof(Period),
        IsUnique = true)]
    public class TimetableEntry
    {
        public int Id { get; set; }

        // Lớp học
        [Range(1, int.MaxValue)]
        public int SchoolClassId { get; set; }

        public SchoolClass SchoolClass { get; set; } = null!;

        // Thứ 2 đến thứ 7
        [Range(2, 7)]
        public int DayOfWeek { get; set; }

        // Tiết 1 đến tiết 10
        [Range(1, 10)]
        public int Period { get; set; }

        // Tên môn học
        [Required(ErrorMessage = "Vui lòng nhập môn học")]
        [StringLength(100)]
        public string SubjectName { get; set; } = "";

        // Giáo viên bộ môn
        [StringLength(100)]
        public string? TeacherName { get; set; }

        // Phòng học
        [StringLength(30)]
        public string? Room { get; set; }
    }
}
