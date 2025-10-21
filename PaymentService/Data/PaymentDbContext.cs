using Microsoft.EntityFrameworkCore;
using PaymentService.Entities;

namespace PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Payment entity configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);
            
            entity.Property(p => p.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(p => p.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            entity.Property(p => p.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("USD");

            entity.Property(p => p.PaymentMethod)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(p => p.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(PaymentStatus.Pending);

            entity.Property(p => p.TransactionId)
                .HasMaxLength(255);

            entity.Property(p => p.GatewayResponse)
                .HasMaxLength(1000);

            entity.Property(p => p.Description)
                .HasMaxLength(500);

            entity.Property(p => p.Metadata)
                .HasMaxLength(2000);

            entity.Property(p => p.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(p => p.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Indexes
            entity.HasIndex(p => p.UserId)
                .HasDatabaseName("IX_Payments_UserId");

            entity.HasIndex(p => p.OrderId)
                .HasDatabaseName("IX_Payments_OrderId");

            entity.HasIndex(p => p.Status)
                .HasDatabaseName("IX_Payments_Status");

            entity.HasIndex(p => p.CreatedAt)
                .HasDatabaseName("IX_Payments_CreatedAt");

            entity.HasIndex(p => p.TransactionId)
                .HasDatabaseName("IX_Payments_TransactionId")
                .IsUnique()
                .HasFilter("[TransactionId] IS NOT NULL");

            // Relationships
            entity.HasMany(p => p.Refunds)
                .WithOne(r => r.Payment)
                .HasForeignKey(r => r.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Refund entity configuration
        modelBuilder.Entity<Refund>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.PaymentId)
                .IsRequired();

            entity.Property(r => r.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            entity.Property(r => r.Reason)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(r => r.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(RefundStatus.Pending);

            entity.Property(r => r.RefundTransactionId)
                .HasMaxLength(255);

            entity.Property(r => r.GatewayResponse)
                .HasMaxLength(1000);

            entity.Property(r => r.AdminNotes)
                .HasMaxLength(1000);

            entity.Property(r => r.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(r => r.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Indexes
            entity.HasIndex(r => r.PaymentId)
                .HasDatabaseName("IX_Refunds_PaymentId");

            entity.HasIndex(r => r.Status)
                .HasDatabaseName("IX_Refunds_Status");

            entity.HasIndex(r => r.CreatedAt)
                .HasDatabaseName("IX_Refunds_CreatedAt");

            entity.HasIndex(r => r.RefundTransactionId)
                .HasDatabaseName("IX_Refunds_RefundTransactionId")
                .IsUnique()
                .HasFilter("[RefundTransactionId] IS NOT NULL");
        });

        // Seed data for development
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            SeedDevelopmentData(modelBuilder);
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<Payment>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        var refundEntries = ChangeTracker.Entries<Refund>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in refundEntries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private static void SeedDevelopmentData(ModelBuilder modelBuilder)
    {
        var paymentId1 = Guid.NewGuid();
        var paymentId2 = Guid.NewGuid();
        var refundId1 = Guid.NewGuid();

        // Seed payments
        modelBuilder.Entity<Payment>().HasData(
            new Payment
            {
                Id = paymentId1,
                UserId = "dev-user-1",
                OrderId = Guid.NewGuid(),
                Amount = 99.99m,
                Currency = "USD",
                PaymentMethod = "credit_card",
                Status = PaymentStatus.Completed,
                TransactionId = "txn_dev_001",
                Description = "Test payment 1",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                ProcessedAt = DateTime.UtcNow.AddDays(-5)
            },
            new Payment
            {
                Id = paymentId2,
                UserId = "dev-user-2",
                OrderId = Guid.NewGuid(),
                Amount = 149.99m,
                Currency = "USD",
                PaymentMethod = "paypal",
                Status = PaymentStatus.Pending,
                Description = "Test payment 2",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            }
        );

        // Seed refunds
        modelBuilder.Entity<Refund>().HasData(
            new Refund
            {
                Id = refundId1,
                PaymentId = paymentId1,
                Amount = 29.99m,
                Reason = "Customer requested partial refund",
                Status = RefundStatus.Completed,
                RefundTransactionId = "ref_dev_001",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                ProcessedAt = DateTime.UtcNow.AddDays(-2)
            }
        );
    }
}
