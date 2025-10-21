using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Entities;

public class Order
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ConfirmedAt { get; set; }
    
    public DateTime? ShippedAt { get; set; }
    
    public DateTime? DeliveredAt { get; set; }
    
    public DateTime? CancelledAt { get; set; }
    
    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal ShippingCost { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    [MaxLength(200)]
    public string? ShippingAddress { get; set; }
    
    [MaxLength(100)]
    public string? PaymentMethod { get; set; }
    
    [MaxLength(100)]
    public string? PaymentTransactionId { get; set; }
    
    // Navigation properties
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    
    // Computed properties
    [NotMapped]
    public decimal SubTotal => Items?.Sum(i => i.Price * i.Quantity) ?? 0;
    
    [NotMapped]
    public int TotalItems => Items?.Sum(i => i.Quantity) ?? 0;
    
    [NotMapped]
    public bool CanBeCancelled => Status == OrderStatus.Pending || Status == OrderStatus.Confirmed;
}

public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Processing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    Refunded = 7
}
