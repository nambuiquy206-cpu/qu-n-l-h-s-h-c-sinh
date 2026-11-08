
using System.ComponentModel.DataAnnotations;

namespace ThptA.ViewModels
{
    public class StudentEditVm
    {
        public int Id { get; set; }

        [Required]
        public string RowVersion { get; set; } = "";

        public string Code { get; set; } = "";

        public string ClassDisplay { get; set; } = "";

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

        [Required(ErrorMessage = "Nhập tên người liên hệ")]
        [StringLength(100)]
        public string ContactFullName { get; set; } = "";

        [Required(ErrorMessage = "Nhập quan hệ")]
        [StringLength(30)]
        public string ContactRelationship { get; set; } = "";

        [Required(ErrorMessage = "Nhập số điện thoại")]
        [RegularExpression(@"^\+?[0-9]{9,15}$",
            ErrorMessage = "Số điện thoại không hợp lệ")]
        public string ContactPhone { get; set; } = "";
    }
}
