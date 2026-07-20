using SafarSuite.ControlDesk.Application.Modules.Auth.RecoverLocalOperatorPassword;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class OperatorRecoveryDecisionPolicyTests
{
    [Fact]
    public void Requires_elevation_and_readable_machine_secret()
    {
        var notElevated = OperatorRecoveryDecisionPolicy.Evaluate(ValidRequest() with { IsElevated = false });
        var unreadable = OperatorRecoveryDecisionPolicy.Evaluate(ValidRequest() with { MachineSecretReadable = false });

        Assert.Equal("elevation-required", notElevated.ErrorCode);
        Assert.Equal("machine-secret-unavailable", unreadable.ErrorCode);
    }

    [Fact]
    public void Requires_actor_and_reason()
    {
        var missingActor = OperatorRecoveryDecisionPolicy.Evaluate(ValidRequest() with { Actor = " " });
        var missingReason = OperatorRecoveryDecisionPolicy.Evaluate(ValidRequest() with { Reason = " " });

        Assert.Equal("actor-required", missingActor.ErrorCode);
        Assert.Equal("reason-required", missingReason.ErrorCode);
    }

    [Fact]
    public void Preserves_or_reissues_machine_secret_by_explicit_request()
    {
        var preserve = OperatorRecoveryDecisionPolicy.Evaluate(ValidRequest());
        var reissue = OperatorRecoveryDecisionPolicy.Evaluate(ValidRequest() with { ReissueMachineSecret = true });

        Assert.True(preserve.IsAllowed);
        Assert.False(preserve.ShouldReissueMachineSecret);
        Assert.True(reissue.ShouldReissueMachineSecret);
    }

    private static OperatorRecoveryRequest ValidRequest() => new(
        IsElevated: true,
        MachineSecretReadable: true,
        ReissueMachineSecret: false,
        Actor: "offline-recovery",
        Reason: "Owner-approved recovery drill",
        NewPassword: "correct-horse-battery-staple");
}
