namespace PaymentService.DTOs
{
    public class RefundRequestDto
    {
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
