using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs;

public class OrderItemDto
{
    [Required]
    [MaxLength(100)]
    public string ProductId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? ProductDescription { get; set; }
    
    [MaxLength(200)]
    public string? ProductImageUrl { get; set; }
    
    [MaxLength(50)]
    public string? ProductSku { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Discount amount cannot be negative")]
    public decimal DiscountAmount { get; set; }
}
