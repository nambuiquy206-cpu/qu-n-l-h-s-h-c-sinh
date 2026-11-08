
namespace ThptA.ViewModels
{
    public class FeePaymentHistoryVm
    {
        public int StudentId { get; set; }
        public string StudentDisplay { get; set; } = "";
        public string FeeTitle { get; set; } = "";
        public string YearCode { get; set; } = "";

        public decimal AmountDue { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }

        public List<FeePaymentHistoryRowVm> Payments
        { get; set; } = new();
    }

    public class FeePaymentHistoryRowVm
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAtUtc { get; set; }
        public string RecordedBy { get; set; } = "";
        public string? Note { get; set; }
    }
}
