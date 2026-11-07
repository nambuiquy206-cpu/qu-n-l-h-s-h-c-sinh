
using System.ComponentModel.DataAnnotations;

namespace ThptA.ViewModels
{
    public class LoginVm
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }
}
