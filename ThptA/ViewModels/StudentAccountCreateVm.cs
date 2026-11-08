
using System.ComponentModel.DataAnnotations;

namespace ThptA.ViewModels
{
    public class StudentAccountCreateVm
    {
        public int StudentId { get; set; }

        public string StudentDisplay { get; set; } = "";

        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Hai mật khẩu không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";
    }
}
