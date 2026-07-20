using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Domain.Tests;

public sealed class LocalOperatorTests
{
    [Fact]
    public void Email_normalizes_identity_without_changing_display_value()
    {
        var email = LocalOperatorEmail.Create("  Operator.Name@example.test  ");

        Assert.Equal("Operator.Name@example.test", email.Value);
        Assert.Equal("OPERATOR.NAME@EXAMPLE.TEST", email.NormalizedValue);
        Assert.Equal(email, LocalOperatorEmail.Create("operator.name@EXAMPLE.TEST"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@example.test")]
    [InlineData("operator@")]
    [InlineData("two@@example.test")]
    [InlineData("space in@example.test")]
    public void Email_rejects_values_outside_the_persistence_identity_boundary(string value)
    {
        Assert.Throws<ArgumentException>(() => LocalOperatorEmail.Create(value));
    }

    [Fact]
    public void First_administrator_has_canonical_access_and_initial_security_version()
    {
        var operatorUser = CreateAdministrator();

        Assert.Equal(LocalOperatorStatus.Active, operatorUser.Status);
        Assert.Equal(1, operatorUser.SecurityVersion);
        Assert.Equal([LocalOperatorRole.Administrator], operatorUser.Roles);
        Assert.Equal([LocalOperatorScope.Admin], operatorUser.Scopes);
        Assert.Equal(CreatedAt, operatorUser.CreatedAtUtc);
        Assert.Equal(CreatedAt, operatorUser.UpdatedAtUtc);
    }

    [Fact]
    public void Create_rejects_an_empty_operator_id()
    {
        Assert.Throws<ArgumentException>(() => LocalOperator.CreateFirstAdministrator(
            default,
            LocalOperatorEmail.Create("admin@example.test"),
            "Office Administrator",
            PasswordHash,
            CreatedAt));
    }

    [Fact]
    public void Access_is_canonical_deduplicated_and_stably_ordered()
    {
        var operatorUser = LocalOperator.Create(
            LocalOperatorId.Create(Guid.Parse("f6ef17f0-84a6-47ca-9b8a-2484d98f9879")),
            LocalOperatorEmail.Create("finance@example.test"),
            "Finance Operator",
            PasswordHash,
            [" financeoperator ", "FINANCEOPERATOR"],
            ["payments:manage", " BILLING:MANAGE ", "payments:manage"],
            CreatedAt);

        Assert.Equal([LocalOperatorRole.FinanceOperator], operatorUser.Roles);
        Assert.Equal(
            [LocalOperatorScope.BillingManage, LocalOperatorScope.PaymentsManage],
            operatorUser.Scopes);
    }

    [Theory]
    [InlineData("UnknownRole", "billing:manage")]
    [InlineData("FinanceOperator", "unknown:scope")]
    public void Create_rejects_unknown_roles_or_scopes(string role, string scope)
    {
        Assert.Throws<ArgumentException>(() => LocalOperator.Create(
            LocalOperatorId.Create(Guid.NewGuid()),
            LocalOperatorEmail.Create("operator@example.test"),
            "Operator",
            PasswordHash,
            [role],
            [scope],
            CreatedAt));
    }

    [Theory]
    [InlineData("Administrator", "billing:manage")]
    [InlineData("FinanceOperator", "control-desk:admin")]
    public void Create_rejects_mismatched_administrator_role_and_scope(string role, string scope)
    {
        Assert.Throws<ArgumentException>(() => LocalOperator.Create(
            LocalOperatorId.Create(Guid.NewGuid()),
            LocalOperatorEmail.Create("operator@example.test"),
            "Operator",
            PasswordHash,
            [role],
            [scope],
            CreatedAt));
    }

    [Fact]
    public void Protected_changes_increment_security_version_and_update_timestamp()
    {
        var operatorUser = CreateAdministrator();

        operatorUser.ChangePasswordHash("replacement-password-hash", CreatedAt.AddMinutes(1));
        operatorUser.ChangeAccess(
            [LocalOperatorRole.SupportOperator],
            [LocalOperatorScope.DiagnosticsRead],
            CreatedAt.AddMinutes(2));
        operatorUser.Disable(CreatedAt.AddMinutes(3));

        Assert.Equal(4, operatorUser.SecurityVersion);
        Assert.Equal(LocalOperatorStatus.Disabled, operatorUser.Status);
        Assert.Equal([LocalOperatorRole.SupportOperator], operatorUser.Roles);
        Assert.Equal([LocalOperatorScope.DiagnosticsRead], operatorUser.Scopes);
        Assert.Equal(CreatedAt.AddMinutes(3), operatorUser.UpdatedAtUtc);
    }

    [Fact]
    public void Repeating_the_same_state_does_not_invalidate_sessions()
    {
        var operatorUser = CreateAdministrator();

        operatorUser.Rename("Office Administrator", CreatedAt.AddMinutes(1));
        operatorUser.ChangePasswordHash(PasswordHash, CreatedAt.AddMinutes(1));
        operatorUser.ChangeAccess(
            [LocalOperatorRole.Administrator],
            [LocalOperatorScope.Admin],
            CreatedAt.AddMinutes(1));
        operatorUser.Enable(CreatedAt.AddMinutes(1));

        Assert.Equal(1, operatorUser.SecurityVersion);
        Assert.Equal(CreatedAt, operatorUser.UpdatedAtUtc);
    }

    [Fact]
    public void Change_rejects_time_before_the_last_protected_change()
    {
        var operatorUser = CreateAdministrator();
        operatorUser.Disable(CreatedAt.AddMinutes(2));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            operatorUser.Enable(CreatedAt.AddMinutes(1)));
    }

    [Fact]
    public void Create_rejects_non_utc_timestamps()
    {
        Assert.Throws<ArgumentException>(() => LocalOperator.CreateFirstAdministrator(
            LocalOperatorId.Create(Guid.NewGuid()),
            LocalOperatorEmail.Create("admin@example.test"),
            "Office Administrator",
            PasswordHash,
            CreatedAt.ToOffset(TimeSpan.FromHours(5))));
    }

    private static LocalOperator CreateAdministrator() =>
        LocalOperator.CreateFirstAdministrator(
            LocalOperatorId.Create(Guid.Parse("50d33048-1c1c-49c9-8b7d-28c69f6d5c29")),
            LocalOperatorEmail.Create("admin@example.test"),
            "Office Administrator",
            PasswordHash,
            CreatedAt);

    private const string PasswordHash =
        "pbkdf2-sha256.120000.AQIDBAUGBwgJCgsMDQ4PEA.bKfX3l_4QOvv59HDi9Wq1UzY3FYjDWr3w5qQgkLufc4";

    private static readonly DateTimeOffset CreatedAt =
        new(2026, 7, 20, 3, 10, 0, TimeSpan.Zero);
}
