
using System.ComponentModel.DataAnnotations;

namespace ThptA.ViewModels
{
    public class TimetableEditVm
    {
        public int Id { get; set; }

        public string ClassDisplay { get; set; } = "";

        public int DayOfWeek { get; set; }

        public int Period { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập môn học")]
        [StringLength(100)]
        public string SubjectName { get; set; } = "";

        [StringLength(100)]
        public string? TeacherName { get; set; }

        [StringLength(30)]
        public string? Room { get; set; }
    }
}
