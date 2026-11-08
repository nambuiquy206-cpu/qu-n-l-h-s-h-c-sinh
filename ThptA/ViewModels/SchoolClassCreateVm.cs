
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ThptA.ViewModels
{
    public class SchoolClassCreateVm
    {
        [Required(ErrorMessage = "Vui lòng nhập tên lớp")]
        [StringLength(20)]
        public string Name { get; set; } = "";

        [Range(10, 12,
            ErrorMessage = "Khối phải từ 10 đến 12")]
        public int Grade { get; set; } = 10;

        [Range(1, int.MaxValue,
            ErrorMessage = "Vui lòng chọn năm học")]
        public int AcademicYearId { get; set; }

        public List<SelectListItem> YearOptions { get; set; }
            = new();
    }
}

