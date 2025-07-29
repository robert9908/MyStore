using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Entities;
using OrderService.Interfaces;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly OrderDbContext _context;

        public OrdersController(IOrderService orderService, OrderDbContext context)
        {
            _orderService = orderService;
            _context = context;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var userId = User.FindFirst("sub")?.Value;
            if (userId == null)
                return Unauthorized();
            var order = new Order
            {
                userId = userId,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                TotalAmount = request.TotalAmount,
                Items = request.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id },
           order);
        }        [HttpPost("{id}/confirm")]
        [Authorize]
        public async Task<IActionResult> ConfirmOrder(Guid id)
        {
            var userId = User.FindFirst("sub")?.Value;
            if (userId == null)
                return Unauthorized();
            var order = await _context.Orders.Include(o =>
           o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
                return NotFound();
            if (order.userId != userId)
                return Forbid();
            if (order.Status != OrderStatus.Pending)
                return BadRequest("Order already confirmed or cancelled");
            order.Status = OrderStatus.Confirmed;
            order.ConfirmedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(order);
        }


        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var userId = User.FindFirst("sub")?.Value;
            if (userId == null)
                return Unauthorized();
            var order = await _context.Orders.Include(o =>
           o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
                return NotFound();
            if (order.userId != userId && !User.IsInRole("Admin"))
                return Forbid();
            return Ok(order);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = User.FindFirst("sub")?.Value ?? "user-id-placeholder";
            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(orders);
        }


        [HttpGet("admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            // Добавить авторизацию (например, [Authorize(Roles = "Admin")])
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

    }
}
