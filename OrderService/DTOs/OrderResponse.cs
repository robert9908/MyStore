using OrderService.Entities;

namespace OrderService.DTOs;

public class OrderResponse
{
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TaxAmount { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentTransactionId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public List<OrderItemResponse> Items { get; set; } = new();
        
        // Computed properties
        public decimal SubTotal => Items.Sum(i => i.LineTotal);
        public int TotalItems => Items.Sum(i => i.Quantity);
        public bool CanBeCancelled => Status == OrderStatus.Pending || Status == OrderStatus.Confirmed;
    }

public class OrderItemResponse
{
    public Guid Id { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public string? ProductImageUrl { get; set; }
    public string? ProductSku { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
}
