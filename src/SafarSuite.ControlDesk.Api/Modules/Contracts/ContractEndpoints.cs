using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.Contracts;
using SafarSuite.ControlDesk.Application.Modules.Contracts.CreateClientContract;
using SafarSuite.ControlDesk.Application.Modules.Contracts.GetClientContract;
using SafarSuite.ControlDesk.Application.Modules.Contracts.ListClientContracts;
using SafarSuite.ControlDesk.Application.Modules.Contracts.ListProductAccessCatalog;
using SafarSuite.ControlDesk.Application.Modules.Contracts.ListProductModules;
using SafarSuite.ControlDesk.Application.Modules.Contracts.PublishProductAccessCatalogCommand;
using SafarSuite.ControlDesk.Application.Modules.Contracts.ReplaceActiveClientContract;
using SafarSuite.ControlDesk.Application.Modules.Contracts.SaveProductAccessCatalog;
using SafarSuite.ControlDesk.Application.Modules.Contracts.SuspendClientContract;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Contracts;

namespace SafarSuite.ControlDesk.Api.Modules.Contracts;

public static class ContractEndpoints
{
    public static IEndpointRouteBuilder MapContractEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/contracts")
            .WithTags("Contracts");

        group.MapPost("/client-contracts", CreateClientContractAsync);
        group.MapGet("/client-contracts/{contractId:guid}", GetClientContractAsync);
        group.MapPost("/client-contracts/{contractId:guid}/suspend", SuspendClientContractAsync);
        group.MapPost("/client-contracts/replace-active", ReplaceActiveClientContractAsync);
        group.MapGet("/clients/{clientId:guid}/client-contracts", ListClientContractsAsync);
        group.MapGet("/product-modules", ListProductModulesAsync);
        group.MapGet("/product-access-catalog", ListProductAccessCatalogAsync);
        group.MapPut("/product-access-catalog", SaveProductAccessCatalogAsync);
        group.MapPost("/product-access-catalog/product-kernel-command", PublishProductAccessCatalogCommandAsync);

        return endpoints;
    }

    private static async Task<IResult> SaveProductAccessCatalogAsync(
        SaveProductAccessCatalogRequest request,
        SaveProductAccessCatalogHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new SaveProductAccessCatalogCommand(
                (request.ModuleGroups ?? []).Select(group => new SaveProductModuleGroupCommand(
                        group.GroupId,
                        group.DisplayName,
                        group.AccessKind,
                        (group.ModuleCodes ?? []).ToArray()))
                    .ToArray(),
                (request.Resources ?? []).Select(resource => new SaveProductResourceCommand(
                        resource.ResourceId,
                        resource.DisplayName,
                        resource.AccessKind,
                        (resource.RequiredGroupIds ?? []).ToArray(),
                        (resource.RequiredModuleCodes ?? []).ToArray()))
                    .ToArray(),
                request.RequestedBy ?? string.Empty),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ProductAccessCatalogResponse(
            result.Value.ModuleGroups.Select(group => new ProductModuleGroupResponse(
                    group.GroupId,
                    group.DisplayName,
                    group.AccessKind,
                    group.ModuleCodes.ToArray()))
                .ToArray(),
            result.Value.Resources.Select(resource => new ProductResourceResponse(
                    resource.ResourceId,
                    resource.DisplayName,
                    resource.AccessKind,
                    resource.RequiredGroupIds.ToArray(),
                    resource.RequiredModuleCodes.ToArray(),
                    resource.ResolvedModuleCodes.ToArray()))
                .ToArray()));
    }

    private static async Task<IResult> PublishProductAccessCatalogCommandAsync(
        PublishProductAccessCatalogCommandRequest request,
        PublishProductAccessCatalogCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new PublishProductAccessCatalogCommand(
                request.ActivationRequestId,
                request.ExpiresInHours ?? 2,
                request.RequestedBy),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new PublishProductAccessCatalogCommandResponse(
            result.Value.CommandId,
            result.Value.ServerInstallationId,
            result.Value.CommandType,
            result.Value.ProductKernelCommand,
            result.Value.Signature,
            result.Value.SigningKeyId,
            result.Value.ExpiresAt,
            new ProductAccessCatalogResponse(
                result.Value.ModuleGroups.Select(group => new ProductModuleGroupResponse(
                        group.GroupId,
                        group.DisplayName,
                        group.AccessKind,
                        group.ModuleCodes.ToArray()))
                    .ToArray(),
                result.Value.Resources.Select(resource => new ProductResourceResponse(
                        resource.ResourceId,
                        resource.DisplayName,
                        resource.AccessKind,
                        resource.RequiredGroupIds.ToArray(),
                        resource.RequiredModuleCodes.ToArray(),
                        resource.ResolvedModuleCodes.ToArray()))
                    .ToArray())));
    }

    private static async Task<IResult> ListProductAccessCatalogAsync(
        ListProductAccessCatalogHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ProductAccessCatalogResponse(
            result.Value.ModuleGroups.Select(group => new ProductModuleGroupResponse(
                    group.GroupId,
                    group.DisplayName,
                    group.AccessKind,
                    group.ModuleCodes.ToArray()))
                .ToArray(),
            result.Value.Resources.Select(resource => new ProductResourceResponse(
                    resource.ResourceId,
                    resource.DisplayName,
                    resource.AccessKind,
                    resource.RequiredGroupIds.ToArray(),
                    resource.RequiredModuleCodes.ToArray(),
                    resource.ResolvedModuleCodes.ToArray()))
                .ToArray()));
    }

    private static async Task<IResult> ListProductModulesAsync(
        ListProductModulesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ListProductModulesResponse(
            result.Value.Modules.Select(module => new ProductModuleResponse(
                    module.ModuleCode,
                    module.DisplayName,
                    module.CommercialMode,
                    module.IsActive,
                    module.BillingDefaults is null ? null : new ProductModuleBillingDefaultsResponse(
                        module.BillingDefaults.ChargeCode,
                        module.BillingDefaults.ChargeName,
                        module.BillingDefaults.Description,
                        module.BillingDefaults.DefaultUnitPriceAmount,
                        module.BillingDefaults.CurrencyCode,
                        module.BillingDefaults.BillingCycle)))
                .ToArray()));
    }

    private static async Task<IResult> CreateClientContractAsync(
        CreateClientContractRequest request,
        CreateClientContractHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateClientContractCommand(
            request.ClientId,
            request.ContractNumber,
            request.StartsOn,
            request.EndsOn,
            request.RecurringAmount,
            request.CurrencyCode,
            request.BillingCycle,
            request.BillingDayOfMonth,
            request.AllowedDevices,
            request.AllowedBranches,
            request.Modules.Select(module => new CreateClientContractModuleCommand(
                module.ModuleCode,
                module.IsEnabled)).ToArray());

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new CreateClientContractResponse(
            result.Value.ContractId,
            result.Value.ClientId,
            result.Value.ContractNumber,
            result.Value.StartsOn,
            result.Value.EndsOn,
            result.Value.RecurringAmount,
            result.Value.CurrencyCode,
            result.Value.BillingCycle,
            result.Value.BillingDayOfMonth,
            result.Value.AllowedDevices,
            result.Value.AllowedBranches,
            result.Value.Status,
            result.Value.CreatedAtUtc,
            result.Value.ActivatedAtUtc,
            result.Value.Modules.Select(module => new ClientContractModuleResponse(
                module.ModuleCode,
                module.IsEnabled)).ToArray());

        return Results.Created($"/api/v1/contracts/client-contracts/{response.ContractId}", response);
    }

    private static async Task<IResult> GetClientContractAsync(
        Guid contractId,
        GetClientContractHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetClientContractQuery(contractId), cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(ToContractResponse(result.Value));
    }

    private static async Task<IResult> ListClientContractsAsync(
        Guid clientId,
        ListClientContractsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListClientContractsQuery(clientId), cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ListClientContractsResponse(
            result.Value.ClientId,
            result.Value.Contracts.Select(ToContractResponse).ToArray()));
    }

    private static async Task<IResult> SuspendClientContractAsync(
        Guid contractId,
        SuspendClientContractHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new SuspendClientContractCommand(contractId), cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(ToContractResponse(result.Value));
    }

    private static async Task<IResult> ReplaceActiveClientContractAsync(
        CreateClientContractRequest request,
        ReplaceActiveClientContractHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new ReplaceActiveClientContractCommand(
            request.ClientId,
            request.ContractNumber,
            request.StartsOn,
            request.EndsOn,
            request.RecurringAmount,
            request.CurrencyCode,
            request.BillingCycle,
            request.BillingDayOfMonth,
            request.AllowedDevices,
            request.AllowedBranches,
            request.Modules.Select(module => new ReplaceActiveClientContractModuleCommand(
                module.ModuleCode,
                module.IsEnabled)).ToArray());

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ReplaceActiveClientContractResponse(
            result.Value.SuspendedContract is null ? null : ToContractResponse(result.Value.SuspendedContract),
            ToContractResponse(result.Value.ActiveContract)));
    }

    private static ClientContractResponse ToContractResponse(ClientContractResult contract)
    {
        return new ClientContractResponse(
            contract.ContractId,
            contract.ClientId,
            contract.ContractNumber,
            contract.StartsOn,
            contract.EndsOn,
            contract.RecurringAmount,
            contract.CurrencyCode,
            contract.BillingCycle,
            contract.BillingDayOfMonth,
            contract.AllowedDevices,
            contract.AllowedBranches,
            contract.Status,
            contract.CreatedAtUtc,
            contract.ActivatedAtUtc,
            contract.Modules.Select(module => new ClientContractModuleResponse(
                module.ModuleCode,
                module.IsEnabled)).ToArray());
    }
}
