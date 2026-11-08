
using System.ComponentModel.DataAnnotations;

namespace ThptA.ViewModels
{
    public class TeacherAccountVm
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(50, MinimumLength = 3)]
        [RegularExpression(
            @"^[a-zA-Z0-9._-]+$",
            ErrorMessage = "Tên đăng nhập chỉ gồm chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang")]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập họ tên giáo viên")]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Hai mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = "";
    }
}
