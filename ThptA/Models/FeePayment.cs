
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ThptA.Models
{
    public class FeePayment
    {
        public int Id { get; set; }

        // Khoản thu được thanh toán
        public int StudentFeeId { get; set; }

        public StudentFee StudentFee { get; set; }
            = null!;

        // Số tiền ghi nhận đã đóng
        [Precision(18, 2)]
        [Range(typeof(decimal), "0.01", "999999999999.99")]
        public decimal Amount { get; set; }

        // Thời điểm ghi nhận
        public DateTime PaidAtUtc { get; set; }
            = DateTime.UtcNow;

        // Người thực hiện ghi nhận
        [Required]
        public string RecordedById { get; set; } = "";

        // Ghi chú
        [StringLength(250)]
        public string? Note { get; set; }
    }
}
