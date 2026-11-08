
using System.ComponentModel.DataAnnotations;

namespace ThptA.ViewModels
{
    public class StudentFeeCreateVm
    {
        public int StudentId { get; set; }

        public string StudentDisplay { get; set; } = "";

        public string AcademicYearCode { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập tên khoản thu")]
        [StringLength(120)]
        public string Title { get; set; } = "";

        [Range(typeof(decimal), "0.01", "999999999999.99",
            ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal AmountDue { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hạn đóng")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }
    }
}
