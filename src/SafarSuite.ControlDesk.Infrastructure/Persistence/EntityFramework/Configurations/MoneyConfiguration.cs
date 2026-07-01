using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal static class MoneyConfiguration
{
    public static void Configure<TOwner>(
        OwnedNavigationBuilder<TOwner, Money> money,
        string amountColumnName,
        string currencyCodeColumnName)
        where TOwner : class
    {
        money.Property(value => value.Amount)
            .HasColumnName(amountColumnName)
            .HasPrecision(18, 2)
            .IsRequired();

        money.Property(value => value.CurrencyCode)
            .HasColumnName(currencyCodeColumnName)
            .HasMaxLength(3)
            .IsRequired();
    }
}
