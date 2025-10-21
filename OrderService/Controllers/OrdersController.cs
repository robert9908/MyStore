using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Entities;
using OrderService.Interfaces;
using OrderService.Services;
using System.Security.Claims;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ISecurityService _securityService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderService orderService,
        ISecurityService securityService,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _securityService = securityService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (!await _securityService.IsAuthorizedAsync(User, "orders", "create"))
        {
            return Forbid();
        }

        var userId = _securityService.GetCurrentUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        try
        {
            var order = await _orderService.CreateOrderAsync(userId, request);
            _logger.LogInformation("Order {OrderId} created by user {UserId}", order.Id, userId);
            
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { 
                Message = "Validation failed", 
                Errors = ex.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage }) 
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        if (!await _securityService.CanAccessOrderAsync(User, id))
        {
            return Forbid();
        }

        var userId = _securityService.IsAdmin(User) ? null : _securityService.GetCurrentUserId(User);
        var order = await _orderService.GetOrderByIdAsync(id, userId);
        
        if (order == null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    /// <summary>
    /// Get current user's orders with pagination
    /// </summary>
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(List<OrderResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!await _securityService.IsAuthorizedAsync(User, "orders", "read"))
        {
            return Forbid();
        }

        var userId = _securityService.GetCurrentUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var orders = await _orderService.GetUserOrdersAsync(userId, page, pageSize);
        
        return Ok(new
        {
            Orders = orders,
            Page = page,
            PageSize = pageSize,
            HasMore = orders.Count == pageSize
        });
    }

    /// <summary>
    /// Get all orders (Admin only) with pagination
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<OrderResponse>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!await _securityService.IsAuthorizedAsync(User, "orders", "read"))
        {
            return Forbid();
        }

        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var orders = await _orderService.GetAllOrdersAsync(page, pageSize);
        
        return Ok(new
        {
            Orders = orders,
            Page = page,
            PageSize = pageSize,
            HasMore = orders.Count == pageSize
        });
    }

    /// <summary>
    /// Update order status (Admin only)
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        if (!await _securityService.IsAuthorizedAsync(User, "orders", "update"))
        {
            return Forbid();
        }

        var adminUserId = _securityService.GetCurrentUserId(User);
        var result = await _orderService.UpdateOrderStatusAsync(id, request.Status, adminUserId);
        
        if (!result)
        {
            return NotFound("Order not found or invalid status transition");
        }

        _logger.LogInformation("Order {OrderId} status updated to {Status} by admin {AdminUserId}", 
            id, request.Status, adminUserId);

        return Ok(new { Message = "Order status updated successfully", Status = request.Status });
    }

    /// <summary>
    /// Cancel order (Customer can cancel their own orders, Admin can cancel any)
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderRequest? request = null)
    {
        if (!await _securityService.CanModifyOrderAsync(User, id))
        {
            return Forbid("You can only cancel your own orders and only if they are in a cancellable state");
        }

        var userId = _securityService.GetCurrentUserId(User);
        var result = await _orderService.CancelOrderAsync(id, userId);
        
        if (!result)
        {
            return BadRequest("Order cannot be cancelled or was not found");
        }

        _logger.LogInformation("Order {OrderId} cancelled by user {UserId}. Reason: {Reason}", 
            id, userId, request?.Reason ?? "No reason provided");

        return Ok(new { Message = "Order cancelled successfully" });
    }
}
