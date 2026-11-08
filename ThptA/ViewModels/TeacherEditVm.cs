
using System.ComponentModel.DataAnnotations;

namespace ThptA.ViewModels
{
    public class TeacherEditVm
    {
        [Required]
        public string Id { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string FullName { get; set; } = "";
    }
}
