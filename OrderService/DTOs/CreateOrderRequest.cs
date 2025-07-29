namespace OrderService.DTOs
{
    public class CreateOrderRequest
    {
        public decimal TotalAmount { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }
}
