using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Contracts.SafarSuiteApp.V1;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.PublishProductAccessCatalogCommand;

public sealed class PublishProductAccessCatalogCommandHandler
{
    private const int MaximumCommandLifetimeHours = 168;

    private readonly IProductModuleCatalog _catalog;
    private readonly IProductKernelCommandIssuerClient _commandIssuer;
    private readonly IClock _clock;

    public PublishProductAccessCatalogCommandHandler(
        IProductModuleCatalog catalog,
        IProductKernelCommandIssuerClient commandIssuer,
        IClock clock)
    {
        _catalog = catalog;
        _commandIssuer = commandIssuer;
        _clock = clock;
    }

    public async Task<Result<PublishProductAccessCatalogCommandResult>> HandleAsync(
        PublishProductAccessCatalogCommand command,
        CancellationToken cancellationToken = default)
    {
        var requestedBy = NormalizeRequiredText(command.RequestedBy, 160);
        var errors = Validate(command.ActivationRequestId, command.ExpiresInHours, requestedBy);

        if (errors.Count > 0)
        {
            return Result<PublishProductAccessCatalogCommandResult>.Failure(errors);
        }

        var catalog = await _catalog.GetAccessCatalogAsync(cancellationToken);
        var appCatalog = new ProductAccessCatalogCommandPayload(
            catalog.ModuleGroups.Select(group => new ProductModuleGroupCommandPayload(
                    group.GroupId,
                    group.DisplayName,
                    group.AccessKind,
                    group.ModuleCodes.ToArray()))
                .ToArray(),
            catalog.Resources.Select(resource => new ProductResourceCommandPayload(
                    resource.ResourceId,
                    resource.DisplayName,
                    resource.AccessKind,
                    resource.RequiredGroupIds.ToArray(),
                    resource.RequiredModuleCodes.ToArray(),
                    resource.ResolvedModuleCodes.ToArray()))
                .ToArray());
        var request = new IssueProductKernelCommandRequest(
            SafarSuiteProductKernelCommandTypes.SetProductAccessCatalog,
            ModuleId: null,
            IsEnabled: null,
            EntitlementStatus: null,
            PaidUntil: null,
            GraceEndsOn: null,
            OfflineValidUntil: null,
            ModuleEntitlements: null,
            ExpiresAt: _clock.UtcNow.AddHours(command.ExpiresInHours),
            AccessCatalog: appCatalog);
        var result = await _commandIssuer.IssueCommandAsync(
            command.ActivationRequestId,
            request,
            requestedBy!,
            cancellationToken);

        if (!result.IsSuccess || result.Command is null)
        {
            return Result<PublishProductAccessCatalogCommandResult>.Failure(ToApplicationError(result));
        }

        return Result<PublishProductAccessCatalogCommandResult>.Success(new PublishProductAccessCatalogCommandResult(
            result.Command.CommandId,
            result.Command.ServerInstallationId,
            result.Command.CommandType,
            result.Command.ProductKernelCommand,
            result.Command.Signature,
            result.Command.SigningKeyId,
            result.Command.ExpiresAt,
            catalog.ModuleGroups.Select(group => new PublishedProductModuleGroupResult(
                    group.GroupId,
                    group.DisplayName,
                    group.AccessKind,
                    group.ModuleCodes.ToArray()))
                .ToArray(),
            catalog.Resources.Select(resource => new PublishedProductResourceResult(
                    resource.ResourceId,
                    resource.DisplayName,
                    resource.AccessKind,
                    resource.RequiredGroupIds.ToArray(),
                    resource.RequiredModuleCodes.ToArray(),
                    resource.ResolvedModuleCodes.ToArray()))
                .ToArray()));
    }

    private static IReadOnlyCollection<ApplicationError> Validate(
        Guid activationRequestId,
        int expiresInHours,
        string? requestedBy)
    {
        var errors = new List<ApplicationError>();

        if (activationRequestId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(activationRequestId),
                "Activation request id is required."));
        }

        if (expiresInHours is < 1 or > MaximumCommandLifetimeHours)
        {
            errors.Add(ApplicationError.Validation(
                nameof(expiresInHours),
                "Product-kernel command expiry must be between 1 and 168 hours."));
        }

        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            errors.Add(ApplicationError.Validation(
                nameof(requestedBy),
                "Requested by is required before publishing the product access catalog."));
        }

        return errors;
    }

    private static ApplicationError ToApplicationError(
        ProductKernelCommandIssueClientResult result)
    {
        var detail = string.IsNullOrWhiteSpace(result.Detail)
            ? "SafarSuite app product-kernel command issue request failed."
            : result.Detail;

        return result.FailureCode switch
        {
            "ActivationRequestNotFound" => ApplicationError.NotFound("activationRequestId", detail),
            "ProductKernelCommandRejected" => ApplicationError.Validation("accessCatalog", detail),
            "ProductKernelCommandUnauthorized" => ApplicationError.Unexpected("SafarSuite app owner API key was rejected."),
            "ProductKernelCommandForbidden" => ApplicationError.Unexpected("SafarSuite app owner API key cannot issue product-kernel commands."),
            "ProductKernelCommandIssuerNotConfigured" => ApplicationError.Unexpected(detail),
            "ProductKernelCommandIssuerUnavailable" => ApplicationError.Unexpected(detail),
            "ProductKernelCommandIssuerResponseInvalid" => ApplicationError.Unexpected(detail),
            _ => ApplicationError.Unexpected(detail)
        };
    }

    private static string? NormalizeRequiredText(
        string value,
        int maxLength)
    {
        var normalized = value.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}

public sealed record PublishProductAccessCatalogCommand(
    Guid ActivationRequestId,
    int ExpiresInHours,
    string RequestedBy);

public sealed record PublishProductAccessCatalogCommandResult(
    Guid CommandId,
    Guid ServerInstallationId,
    string CommandType,
    string ProductKernelCommand,
    string Signature,
    string SigningKeyId,
    DateTimeOffset ExpiresAt,
    IReadOnlyCollection<PublishedProductModuleGroupResult> ModuleGroups,
    IReadOnlyCollection<PublishedProductResourceResult> Resources);

public sealed record PublishedProductModuleGroupResult(
    string GroupId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> ModuleCodes);

public sealed record PublishedProductResourceResult(
    string ResourceId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> RequiredGroupIds,
    IReadOnlyCollection<string> RequiredModuleCodes,
    IReadOnlyCollection<string> ResolvedModuleCodes);
