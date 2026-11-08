
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ThptA.ViewModels
{
    public class StudentCreateVm
    {
        [Required(ErrorMessage = "Vui lòng nhập mã học sinh")]
        [StringLength(20)]
        public string Code { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Required]
        [RegularExpression("^(M|F|O)$")]
        public string Gender { get; set; } = "M";

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [StringLength(250)]
        public string Address { get; set; } = "";

        [Range(1, int.MaxValue,
            ErrorMessage = "Vui lòng chọn lớp")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Nhập tên người liên hệ")]
        [StringLength(100)]
        public string ContactFullName { get; set; } = "";

        [Required(ErrorMessage = "Nhập quan hệ với học sinh")]
        [StringLength(30)]
        public string ContactRelationship { get; set; } = "";

        [Required(ErrorMessage = "Nhập số điện thoại")]
        [RegularExpression(@"^\+?[0-9]{9,15}$",
            ErrorMessage = "Số điện thoại không hợp lệ")]
        public string ContactPhone { get; set; } = "";

        // Danh sách lớp được phép chọn
        public List<SelectListItem> ClassOptions { get; set; }
            = new();
    }
}
