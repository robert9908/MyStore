using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Entities;

public class OrderItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid OrderId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ProductId { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? ProductDescription { get; set; }
    
    [MaxLength(200)]
    public string? ProductImageUrl { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }
    
    [MaxLength(50)]
    public string? ProductSku { get; set; }
    
    // Navigation properties
    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; } = null!;
    
    // Computed properties
    [NotMapped]
    public decimal LineTotal => (Price * Quantity) - DiscountAmount;
    
    [NotMapped]
    public decimal UnitPriceAfterDiscount => Price - (DiscountAmount / Quantity);
}
