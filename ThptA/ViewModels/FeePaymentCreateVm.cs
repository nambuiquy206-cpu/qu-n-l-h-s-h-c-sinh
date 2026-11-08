
using System.ComponentModel.DataAnnotations;

namespace ThptA.ViewModels
{
    public class FeePaymentCreateVm
    {
        public int StudentFeeId { get; set; }

        public int StudentId { get; set; }

        public string StudentDisplay { get; set; } = "";

        public string FeeTitle { get; set; } = "";

        public decimal AmountDue { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal RemainingAmount { get; set; }

        [Range(typeof(decimal), "0.01", "999999999999.99",
            ErrorMessage = "Số tiền đóng phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [StringLength(250)]
        public string? Note { get; set; }
    }
}
