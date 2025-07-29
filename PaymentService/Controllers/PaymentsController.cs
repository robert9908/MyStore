using System.IO.Pipes;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.DTOs;
using PaymentService.Entities;
using PaymentService.Interfaces;

namespace PaymentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService service)
        {
            _paymentService = service;
        }

        [HttpPost("pay")]
        [Authorize]
        public async Task<IActionResult> Pay([FromBody] PaymentRequestDto request)
        {
            var userId = User.FindFirstValue("sub");
            if (userId == null) return Unauthorized();

            var result = await _paymentService.CreatePaymentAsync(userId, request);
            return Ok(result);
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();
            var doc = JsonDocument.Parse(json);
            await _paymentService.HandleWebhookAsync(doc);
            return Ok();
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetPayment(Guid id)
        {
            var userId = User.FindFirstValue("sub");
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsInRole("Admin");
            var payment = await _paymentService.GetPaymentByIdAsync(id, userId, isAdmin);
            if (payment == null) return Forbid();
            return Ok(payment);
        }

        [HttpPost("{id}/refund")]
        [Authorize]
        public async Task<IActionResult> Refund(Guid id, [FromBody] RefundRequestDto dto)
        {
            var userId = User.FindFirstValue("sub");
            if (userId == null) return Unauthorized();

            await _paymentService.RequestRefundAsync(id, userId, dto);
            return NoContent();
        }
    }
}ss
