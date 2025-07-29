namespace OrderService.DTOs
{
    public class OrderItemDto
    {
        public string ProductId { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
