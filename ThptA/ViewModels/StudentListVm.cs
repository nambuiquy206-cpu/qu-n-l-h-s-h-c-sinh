
using Microsoft.AspNetCore.Mvc.Rendering;
using ThptA.Models;

namespace ThptA.ViewModels
{
    public class StudentListVm
    {
        // Điều kiện tìm kiếm
        public string? Q { get; set; }
        public int? YearId { get; set; }
        public int? Grade { get; set; }
        public int? ClassId { get; set; }
        public string? Status { get; set; }

        public bool OnlyMissingContact { get; set; }

        // Dữ liệu hiển thị
        public List<Student> Students { get; set; } = new();

        public List<SelectListItem> YearOptions { get; set; }
            = new();

        public List<SelectListItem> ClassOptions { get; set; }
            = new();

        // Phân trang
        public int Page { get; set; } = 1;
        public int PageCount { get; set; } = 1;
        public int Total { get; set; }

        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public int MissingContactCount { get; set; }

    }
}
