using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Identity.Tests;

internal sealed class IdentityTestClock : IControlCloudClock
{
    public IdentityTestClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; }
}

internal sealed class IdentityTestUnitOfWork : IControlCloudUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default) =>
        await operation(cancellationToken);

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default) =>
        await operation(cancellationToken);
}

internal sealed class IdentityTestRepository : IClientPortalIdentityRepository
{
    public IdentityTestRepository(ControlCloudClientPortalUser user)
    {
        User = user;
    }

    public ControlCloudClientPortalUser User { get; }

    public int SaveUserCount { get; private set; }

    public Task<ControlCloudClientPortalUser?> GetUserByClientAndEmailAsync(
        Guid clientId,
        string email,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ControlCloudClientPortalUser?>(
            User.ClientId == clientId
            && User.Email.Equals(email, StringComparison.OrdinalIgnoreCase)
                ? User
                : null);

    public Task<ControlCloudClientPortalUser?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ControlCloudClientPortalUser?>(
            User.UserId == userId ? User : null);

    public Task SaveUserAsync(
        ControlCloudClientPortalUser user,
        CancellationToken cancellationToken = default)
    {
        Assert.Same(User, user);
        SaveUserCount++;
        return Task.CompletedTask;
    }

    public Task<ControlCloudClientPortalInvitation?> GetInvitationByIdAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ControlCloudClientPortalInvitation?>(null);

    public Task<ControlCloudClientPortalInvitation?> GetInvitationByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ControlCloudClientPortalInvitation?>(null);

    public Task<IReadOnlyCollection<ControlCloudClientPortalInvitation>> ListInvitationsByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<ControlCloudClientPortalInvitation>>([]);

    public Task AddInvitationAsync(
        ControlCloudClientPortalInvitation invitation,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SaveInvitationAsync(
        ControlCloudClientPortalInvitation invitation,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task AddUserAsync(
        ControlCloudClientPortalUser user,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class IdentityTestSessionService : IClientPortalSessionService
{
    private readonly IdentityTestClock _clock;

    public IdentityTestSessionService(IdentityTestClock clock)
    {
        _clock = clock;
    }

    public int CreateSessionCount { get; private set; }

    public int RevokeAllCount { get; private set; }

    public Guid? RevokedUserId { get; private set; }

    public string? RevokeReason { get; private set; }

    public List<string> Operations { get; } = [];

    public Task<CreateClientPortalSessionResult> CreateSessionAsync(
        Guid clientId,
        string role,
        CancellationToken cancellationToken = default) =>
        CreateSessionAsync(Guid.Empty, clientId, role, 1, cancellationToken);

    public Task<CreateClientPortalSessionResult> CreateSessionAsync(
        Guid userId,
        Guid clientId,
        string role,
        int securityVersion,
        CancellationToken cancellationToken = default)
    {
        CreateSessionCount++;
        Operations.Add("CreateSession");
        return Task.FromResult(CreateClientPortalSessionResult.Success(
            userId,
            clientId,
            "replacement-access-token",
            "replacement-refresh-token",
            _clock.UtcNow.AddMinutes(5),
            _clock.UtcNow.AddMinutes(30),
            role));
    }

    public Task<int> RevokeAllForUserAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        RevokeAllCount++;
        RevokedUserId = userId;
        RevokeReason = reason;
        Operations.Add("RevokeAll");
        return Task.FromResult(2);
    }

    public ClientPortalSessionValidationResult Validate(string? authorizationHeader) =>
        ClientPortalSessionValidationResult.Failure("NotUsed", "Not used in this test.");
}
