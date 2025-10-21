using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs;

public class CreateOrderRequest
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
    public decimal TotalAmount { get; set; }
    
    [Required]
    public List<OrderItemDto> Items { get; set; } = new();
    
    [Required]
    [MaxLength(200)]
    public string ShippingAddress { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string PaymentMethod { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Tax amount cannot be negative")]
    public decimal TaxAmount { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Shipping cost cannot be negative")]
    public decimal ShippingCost { get; set; }
}
