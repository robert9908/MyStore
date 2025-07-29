namespace OrderService.Entities
{
    public class Order
    { 
        public Guid Id {  get; set; }
        public string userId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ConfirmedAt { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public List<OrderItem> Items { get; set; } =  new();
        public decimal TotalAmount { get; set; }


    }

    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Cancelled
    }
}
