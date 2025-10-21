using System.ComponentModel.DataAnnotations;
using OrderService.Entities;

namespace OrderService.DTOs;

public class UpdateOrderStatusRequest
{
    [Required]
    public OrderStatus Status { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class CancelOrderRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}
