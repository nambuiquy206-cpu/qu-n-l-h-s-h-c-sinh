
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ThptA.ViewModels
{
    public class StudentTransferVm
    {
        public int StudentId { get; set; }

        public string StudentCode { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string CurrentClassName { get; set; } = "";
        public string AcademicYearCode { get; set; } = "";

        [Range(1, int.MaxValue,
            ErrorMessage = "Vui lòng chọn lớp mới")]
        public int NewClassId { get; set; }

        [Required]
        public string RowVersion { get; set; } = "";

        public List<SelectListItem> ClassOptions { get; set; }
            = new();
    }
}
