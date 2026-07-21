namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudClientCommercialProjectionEntity
{
    public Guid ClientId { get; set; }

    public string CurrencyCode { get; set; } = "PKR";

    public decimal TotalInvoiced { get; set; }

    public decimal TotalPaid { get; set; }

    public decimal TotalCredited { get; set; }

    public decimal TotalRefunded { get; set; }

    public decimal TotalCreditApplied { get; set; }

    public decimal BalanceDue { get; set; }

    public decimal AvailableCredit { get; set; }

    public bool IsPaid { get; set; } = true;

    public DateTimeOffset LastUpdatedAtUtc { get; set; }

    public string? LatestEntitlementJson { get; set; }
}
