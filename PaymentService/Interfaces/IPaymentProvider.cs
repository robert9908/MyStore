namespace PaymentService.Interfaces
{
    public interface IPaymentProvider
    {
        Task<string> CreatePaymentAsync(Guid internalPaymentId, decimal amount, string description);
        Task RefundPaymentAsync(Guid paymentId, decimal amount, string reason);
    }
}
