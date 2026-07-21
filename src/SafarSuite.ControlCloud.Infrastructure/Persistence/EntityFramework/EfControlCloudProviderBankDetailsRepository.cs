using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfControlCloudProviderBankDetailsRepository
    : IControlCloudProviderBankDetailsRepository
{
    private const string ProviderBankDetailsId = "provider";
    private readonly ControlCloudDbContext _dbContext;

    public EfControlCloudProviderBankDetailsRepository(ControlCloudDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<ControlCloudProviderBankDetails?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ProviderBankDetails
            .AsNoTracking()
            .SingleOrDefaultAsync(
                details => details.BankDetailsId == ProviderBankDetailsId,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task SaveAsync(
        ControlCloudProviderBankDetails bankDetails,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ProviderBankDetails
            .SingleOrDefaultAsync(
                details => details.BankDetailsId == ProviderBankDetailsId,
                cancellationToken);

        if (entity is null)
        {
            entity = new ControlCloudProviderBankDetailsEntity
            {
                BankDetailsId = ProviderBankDetailsId
            };
            await _dbContext.ProviderBankDetails.AddAsync(entity, cancellationToken);
        }

        entity.BankName = bankDetails.BankName ?? "";
        entity.AccountTitle = bankDetails.AccountTitle ?? "";
        entity.AccountNumber = bankDetails.AccountNumber ?? "";
        entity.Iban = bankDetails.Iban ?? "";
        entity.BranchOrRoutingInfo = bankDetails.BranchOrRoutingInfo ?? "";
        entity.UpdatedAtUtc = bankDetails.UpdatedAtUtc;
    }

    private static ControlCloudProviderBankDetails ToDomain(
        ControlCloudProviderBankDetailsEntity entity) => new(
            entity.BankName,
            entity.AccountTitle,
            entity.AccountNumber,
            entity.Iban,
            entity.BranchOrRoutingInfo,
            entity.UpdatedAtUtc);
}
