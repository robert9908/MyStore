using Microsoft.EntityFrameworkCore;
using OrderService.Entities;

namespace OrderService.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);
                    
                entity.Property(e => e.TotalAmount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                    
                entity.Property(e => e.ShippingCost)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);
                    
                entity.Property(e => e.TaxAmount)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);
                    
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                    
                entity.Property(e => e.Status)
                    .HasConversion<int>()
                    .IsRequired();
                    
                entity.Property(e => e.Notes)
                    .HasMaxLength(500);
                    
                entity.Property(e => e.ShippingAddress)
                    .HasMaxLength(200);
                    
                entity.Property(e => e.PaymentMethod)
                    .HasMaxLength(100);
                    
                entity.Property(e => e.PaymentTransactionId)
                    .HasMaxLength(100);

                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_Orders_UserId");
                    
                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_Orders_Status");
                    
                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_Orders_CreatedAt");
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.ProductId)
                    .IsRequired()
                    .HasMaxLength(100);
                    
                entity.Property(e => e.ProductName)
                    .HasMaxLength(200);
                    
                entity.Property(e => e.ProductDescription)
                    .HasMaxLength(500);
                    
                entity.Property(e => e.ProductImageUrl)
                    .HasMaxLength(200);
                    
                entity.Property(e => e.ProductSku)
                    .HasMaxLength(50);
                    
                entity.Property(e => e.Price)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                    
                entity.Property(e => e.DiscountAmount)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);
                    
                entity.Property(e => e.Quantity)
                    .IsRequired();

                entity.HasOne(e => e.Order)
                    .WithMany(o => o.Items)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.OrderId)
                    .HasDatabaseName("IX_OrderItems_OrderId");
                    
                entity.HasIndex(e => e.ProductId)
                    .HasDatabaseName("IX_OrderItems_ProductId");
            });

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                SeedData(modelBuilder);
            }
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            var orderId1 = Guid.NewGuid();
            var orderId2 = Guid.NewGuid();

            modelBuilder.Entity<Order>().HasData(
                new Order
                {
                    Id = orderId1,
                    UserId = "test-user-1",
                    Status = OrderStatus.Pending,
                    TotalAmount = 299.99m,
                    ShippingCost = 9.99m,
                    TaxAmount = 24.00m,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    ShippingAddress = "123 Test Street, Test City, TC 12345"
                },
                new Order
                {
                    Id = orderId2,
                    UserId = "test-user-2",
                    Status = OrderStatus.Confirmed,
                    TotalAmount = 149.99m,
                    ShippingCost = 5.99m,
                    TaxAmount = 12.00m,
                    CreatedAt = DateTime.UtcNow.AddHours(-6),
                    ConfirmedAt = DateTime.UtcNow.AddHours(-5),
                    ShippingAddress = "456 Demo Avenue, Demo City, DC 67890"
                }
            );

            modelBuilder.Entity<OrderItem>().HasData(
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId1,
                    ProductId = "PROD-001",
                    ProductName = "Premium Headphones",
                    ProductSku = "HP-PREM-001",
                    Quantity = 1,
                    Price = 199.99m,
                    DiscountAmount = 0
                },
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId1,
                    ProductId = "PROD-002",
                    ProductName = "Wireless Mouse",
                    ProductSku = "MS-WIRE-002",
                    Quantity = 2,
                    Price = 49.99m,
                    DiscountAmount = 0
                },
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId2,
                    ProductId = "PROD-003",
                    ProductName = "Mechanical Keyboard",
                    ProductSku = "KB-MECH-003",
                    Quantity = 1,
                    Price = 129.99m,
                    DiscountAmount = 10.00m
                }
            );
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Order && e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is Order order)
                {
                    switch (order.Status)
                    {
                        case OrderStatus.Confirmed when order.ConfirmedAt == null:
                            order.ConfirmedAt = DateTime.UtcNow;
                            break;
                        case OrderStatus.Shipped when order.ShippedAt == null:
                            order.ShippedAt = DateTime.UtcNow;
                            break;
                        case OrderStatus.Delivered when order.DeliveredAt == null:
                            order.DeliveredAt = DateTime.UtcNow;
                            break;
                        case OrderStatus.Cancelled when order.CancelledAt == null:
                            order.CancelledAt = DateTime.UtcNow;
                            break;
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
