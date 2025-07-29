namespace PaymentService.DTOs
{
    public class PaymentResponseDto
    {
        public Guid Id { get; set; }
        public string ConfirmationUrl { get; set; } = string.Empty;
    }
}
