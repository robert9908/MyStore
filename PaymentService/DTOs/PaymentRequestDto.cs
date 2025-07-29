namespace PaymentService.DTOs
{
    public class PaymentRequestDto
    {
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public Guid UserId { get; set; }

    }
}
