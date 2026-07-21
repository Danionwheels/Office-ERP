using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Domain.Tests;

public sealed class ProviderBankDetailsTests
{
    [Fact]
    public void Create_empty_returns_the_single_unconfigured_settings_record()
    {
        var details = ProviderBankDetails.CreateEmpty();

        Assert.Equal(ProviderBankDetailsId.Singleton, details.Id);
        Assert.False(details.IsConfigured);
        Assert.Equal(string.Empty, details.BankName);
        Assert.Equal(string.Empty, details.AccountTitle);
        Assert.Equal(string.Empty, details.AccountNumber);
        Assert.Equal(string.Empty, details.Iban);
        Assert.Equal(string.Empty, details.BranchOrRoutingInfo);
    }

    [Fact]
    public void Update_configures_and_normalizes_bank_details()
    {
        var details = ProviderBankDetails.CreateEmpty();
        var updatedAt = new DateTimeOffset(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);

        details.Update(
            "  Safar Bank  ",
            "  SafarSuite Provider Office  ",
            null,
            " pk00safr0000000012345678 ",
            "  Main branch / 001  ",
            updatedAt);

        Assert.True(details.IsConfigured);
        Assert.Equal("Safar Bank", details.BankName);
        Assert.Equal("SafarSuite Provider Office", details.AccountTitle);
        Assert.Equal(string.Empty, details.AccountNumber);
        Assert.Equal("PK00SAFR0000000012345678", details.Iban);
        Assert.Equal("Main branch / 001", details.BranchOrRoutingInfo);
        Assert.Equal(updatedAt, details.UpdatedAtUtc);
    }

    [Fact]
    public void Update_with_all_empty_values_clears_configuration()
    {
        var details = ProviderBankDetails.CreateEmpty();
        details.Update(
            "Safar Bank",
            "SafarSuite Provider Office",
            "123456789",
            null,
            "Main branch",
            UpdatedAt);

        var clearedAt = UpdatedAt.AddHours(1);
        details.Update(" ", null, string.Empty, " ", null, clearedAt);

        Assert.False(details.IsConfigured);
        Assert.Equal(string.Empty, details.BankName);
        Assert.Equal(string.Empty, details.AccountTitle);
        Assert.Equal(string.Empty, details.AccountNumber);
        Assert.Equal(string.Empty, details.Iban);
        Assert.Equal(string.Empty, details.BranchOrRoutingInfo);
        Assert.Equal(clearedAt, details.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(null, "SafarSuite Provider Office", "123456789", null)]
    [InlineData("Safar Bank", null, "123456789", null)]
    [InlineData("Safar Bank", "SafarSuite Provider Office", null, null)]
    public void Partial_configuration_rejects_missing_required_identity_or_account_fields(
        string? bankName,
        string? accountTitle,
        string? accountNumber,
        string? iban)
    {
        var details = ProviderBankDetails.CreateEmpty();

        Assert.Throws<ArgumentException>(() => details.Update(
            bankName,
            accountTitle,
            accountNumber,
            iban,
            "Main branch",
            UpdatedAt));
    }

    [Fact]
    public void Update_rejects_values_beyond_the_persistence_boundary()
    {
        var details = ProviderBankDetails.CreateEmpty();

        Assert.Throws<ArgumentException>(() => details.Update(
            new string('B', 161),
            "SafarSuite Provider Office",
            "123456789",
            null,
            null,
            UpdatedAt));
    }

    private static readonly DateTimeOffset UpdatedAt =
        new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);
}
