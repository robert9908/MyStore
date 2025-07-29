using Microsoft.EntityFrameworkCore;
using PaymentService.Entities;

namespace PaymentService.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions options) : base(options) { }
            public DbSet<Payment> Payments => Set<Payment>();
            public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();
    }
}
