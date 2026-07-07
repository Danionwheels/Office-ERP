using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListProviderAccessOperators;

public sealed class ListProviderAccessOperatorsHandler
{
    private readonly IControlCloudProviderAccessClient _providerAccess;

    public ListProviderAccessOperatorsHandler(
        IControlCloudProviderAccessClient providerAccess)
    {
        _providerAccess = providerAccess;
    }

    public async Task<Result<ProviderAccessOperatorsResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _providerAccess.ListOperatorsAsync(cancellationToken);

        return result.IsSuccess
            ? Result<ProviderAccessOperatorsResponse>.Success(result.Response!)
            : Result<ProviderAccessOperatorsResponse>.Failure(
                ProviderAccessOperatorAdminValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail));
    }
}
