namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public interface IProviderAccessOperatorStore
{
    Task<IReadOnlyCollection<ProviderAccessOperator>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<ProviderAccessOperator?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<ProviderAccessOperator?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        ProviderAccessOperator providerOperator,
        CancellationToken cancellationToken = default);
}
