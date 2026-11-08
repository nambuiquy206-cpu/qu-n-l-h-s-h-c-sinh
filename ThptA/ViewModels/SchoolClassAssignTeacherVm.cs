
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ThptA.ViewModels
{
    public class SchoolClassAssignTeacherVm
    {
        public int Id { get; set; }

        public string ClassName { get; set; } = "";

        public string AcademicYearCode { get; set; } = "";

        // Để trống nếu chưa phân công GVCN
        public string? TeacherId { get; set; }

        [Required]
        public string RowVersion { get; set; } = "";

        public List<SelectListItem> TeacherOptions { get; set; }
            = new();
    }
}
