
using System.ComponentModel.DataAnnotations;

namespace ThptA.ViewModels
{
    public class AcademicYearCreateVm
    {
        [Required(ErrorMessage = "Vui lòng nhập mã năm học")]
        [RegularExpression(@"^\d{4}-\d{4}$",
            ErrorMessage = "Mã năm học phải có dạng 2027-2028")]
        public string Code { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
    }
}
