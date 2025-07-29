using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OrderService.DTOs
{
    public class OrderResponse
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = null;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = null!;
        public List<OrderItemDto> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
    }
}
