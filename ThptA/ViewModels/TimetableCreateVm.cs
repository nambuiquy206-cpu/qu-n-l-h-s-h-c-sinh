
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ThptA.ViewModels
{
    public class TimetableCreateVm
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "Vui lòng chọn lớp học")]
        public int SchoolClassId { get; set; }

        [Range(2, 7,
            ErrorMessage = "Chọn thứ từ 2 đến 7")]
        public int DayOfWeek { get; set; } = 2;

        [Range(1, 10,
            ErrorMessage = "Tiết học phải từ 1 đến 10")]
        public int Period { get; set; } = 1;

        [Required(ErrorMessage = "Vui lòng nhập môn học")]
        [StringLength(100)]
        public string SubjectName { get; set; } = "";

        [StringLength(100)]
        public string? TeacherName { get; set; }

        [StringLength(30)]
        public string? Room { get; set; }

        public List<SelectListItem> ClassOptions { get; set; }
            = new();
    }
}
