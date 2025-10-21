using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentService.Entities;

public class Payment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public Guid OrderId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    [MaxLength(255)]
    public string? TransactionId { get; set; }

    [MaxLength(1000)]
    public string? GatewayResponse { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(2000)]
    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    // Computed properties
    [NotMapped]
    public decimal TotalRefunded => Refunds?.Where(r => r.Status == RefundStatus.Completed).Sum(r => r.Amount) ?? 0;

    [NotMapped]
    public decimal AvailableForRefund => Amount - TotalRefunded;

    [NotMapped]
    public bool CanBeRefunded => Status == PaymentStatus.Completed && AvailableForRefund > 0;

    [NotMapped]
    public string StatusDisplayName => Status switch
    {
        PaymentStatus.Pending => "Ожидает оплаты",
        PaymentStatus.Completed => "Оплачено",
        PaymentStatus.Failed => "Ошибка оплаты",
        PaymentStatus.Cancelled => "Отменено",
        PaymentStatus.Refunded => "Возвращено",
        _ => "Неизвестно"
    };
}

public class Refund
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid PaymentId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public RefundStatus Status { get; set; } = RefundStatus.Pending;

    [MaxLength(255)]
    public string? RefundTransactionId { get; set; }

    [MaxLength(1000)]
    public string? GatewayResponse { get; set; }

    [MaxLength(1000)]
    public string? AdminNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(PaymentId))]
    public virtual Payment Payment { get; set; } = null!;

    // Computed properties
    [NotMapped]
    public string StatusDisplayName => Status switch
    {
        RefundStatus.Pending => "Ожидает возврата",
        RefundStatus.Processing => "Обрабатывается",
        RefundStatus.Completed => "Возвращено",
        RefundStatus.Failed => "Ошибка возврата",
        RefundStatus.Cancelled => "Отменено",
        _ => "Неизвестно"
    };
}

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Cancelled = 3,
    Refunded = 4
}

public enum RefundStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
