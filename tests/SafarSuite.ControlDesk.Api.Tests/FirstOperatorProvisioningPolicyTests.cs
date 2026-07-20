using SafarSuite.ControlDesk.Application.Modules.Auth.ProvisionFirstOperator;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class FirstOperatorProvisioningPolicyTests
{
    [Fact]
    public void Requires_elevation()
    {
        var decision = FirstOperatorProvisioningPolicy.Evaluate(ValidRequest() with { IsElevated = false });

        Assert.False(decision.IsAllowed);
        Assert.Equal("elevation-required", decision.ErrorCode);
    }

    [Fact]
    public void Refuses_overwrite_after_first_operator_exists()
    {
        var decision = FirstOperatorProvisioningPolicy.Evaluate(ValidRequest() with { OperatorAlreadyExists = true });

        Assert.False(decision.IsAllowed);
        Assert.Equal("already-provisioned", decision.ErrorCode);
    }

    [Fact]
    public void Rejects_short_password()
    {
        var decision = FirstOperatorProvisioningPolicy.Evaluate(ValidRequest() with { Password = "too-short" });

        Assert.False(decision.IsAllowed);
        Assert.Equal("password-too-short", decision.ErrorCode);
    }

    [Fact]
    public void Allows_valid_first_operator_request()
    {
        var decision = FirstOperatorProvisioningPolicy.Evaluate(ValidRequest());

        Assert.True(decision.IsAllowed);
        Assert.Null(decision.ErrorCode);
        Assert.Null(decision.ErrorMessage);
    }

    private static FirstOperatorProvisioningRequest ValidRequest() => new(
        IsElevated: true,
        OperatorAlreadyExists: false,
        Email: "owner@example.test",
        FullName: "Office Owner",
        Password: "correct-horse-battery-staple");
}
