using Microsoft.EntityFrameworkCore;
using AuthService.Entities;

namespace AuthService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            // Indexes for performance
            entity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_users_email");

            entity.HasIndex(u => u.RefreshTokenHash)
                .HasDatabaseName("IX_users_refresh_token_hash");

            entity.HasIndex(u => u.EmailConfirmationToken)
                .HasDatabaseName("IX_users_email_confirmation_token");

            entity.HasIndex(u => u.PasswordResetToken)
                .HasDatabaseName("IX_users_password_reset_token");

            entity.HasIndex(u => new { u.Provider, u.ProviderUserId })
                .HasDatabaseName("IX_users_provider_user_id");

            entity.HasIndex(u => u.CreatedAt)
                .HasDatabaseName("IX_users_created_at");

            // Configure relationships
            entity.HasMany(u => u.Sessions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserSession entity configuration
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasIndex(s => s.RefreshTokenHash)
                .IsUnique()
                .HasDatabaseName("IX_user_sessions_refresh_token_hash");

            entity.HasIndex(s => s.UserId)
                .HasDatabaseName("IX_user_sessions_user_id");

            entity.HasIndex(s => s.ExpiresAt)
                .HasDatabaseName("IX_user_sessions_expires_at");

            entity.HasIndex(s => new { s.UserId, s.IsRevoked })
                .HasDatabaseName("IX_user_sessions_user_id_is_revoked");
        });

        // Seed default admin user (for development only)
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            var adminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = adminId,
                Email = "admin@mystore.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin",
                IsEmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is User && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var user = (User)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                user.CreatedAt = DateTime.UtcNow;
            }
            
            user.UpdatedAt = DateTime.UtcNow;
        }
    }
}