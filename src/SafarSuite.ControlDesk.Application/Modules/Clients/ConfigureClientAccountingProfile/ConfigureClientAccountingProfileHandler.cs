using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientAccountingProfile;

public sealed class ConfigureClientAccountingProfileHandler
{
    private readonly IClientRepository _clients;
    private readonly IClientAccountingProfileRepository _profiles;
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly ConfigureClientAccountingProfileValidator _validator;

    public ConfigureClientAccountingProfileHandler(
        IClientRepository clients,
        IClientAccountingProfileRepository profiles,
        ILedgerAccountRepository ledgerAccounts,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        ConfigureClientAccountingProfileValidator validator)
    {
        _clients = clients;
        _profiles = profiles;
        _ledgerAccounts = ledgerAccounts;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<ClientAccountingProfileResult>> HandleAsync(
        ConfigureClientAccountingProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<ClientAccountingProfileResult>.Failure(validationErrors);
        }

        try
        {
            var clientId = ClientId.Create(command.ClientId);
            var accountsReceivableAccountId = LedgerAccountId.Create(command.AccountsReceivableAccountId);

            if (await _clients.GetByIdAsync(clientId, cancellationToken) is null)
            {
                return Result<ClientAccountingProfileResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ClientId),
                    "Client was not found."));
            }

            var receivableAccount = await _ledgerAccounts.GetByIdAsync(accountsReceivableAccountId, cancellationToken);

            if (receivableAccount is null)
            {
                return Result<ClientAccountingProfileResult>.Failure(ApplicationError.NotFound(
                    nameof(command.AccountsReceivableAccountId),
                    "Accounts receivable ledger account was not found."));
            }

            var receivableValidation = ValidateReceivableAccount(command, receivableAccount);

            if (receivableValidation.Count > 0)
            {
                return Result<ClientAccountingProfileResult>.Failure(receivableValidation);
            }

            var existingProfile = await _profiles.GetByClientIdAsync(clientId, cancellationToken);
            var profile = existingProfile;

            if (profile is null)
            {
                profile = ClientAccountingProfile.Create(
                    ClientAccountingProfileId.Create(_idGenerator.NewGuid()),
                    clientId,
                    accountsReceivableAccountId,
                    command.DefaultCurrencyCode,
                    command.CloudCustomerId,
                    _clock.UtcNow);

                await _profiles.AddAsync(profile, cancellationToken);
            }
            else
            {
                profile.Update(
                    accountsReceivableAccountId,
                    command.DefaultCurrencyCode,
                    command.CloudCustomerId,
                    _clock.UtcNow);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ClientAccountingProfileResult>.Success(ToResult(profile));
        }
        catch (ArgumentException exception)
        {
            return Result<ClientAccountingProfileResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }

    private static IReadOnlyCollection<ApplicationError> ValidateReceivableAccount(
        ConfigureClientAccountingProfileCommand command,
        LedgerAccount receivableAccount)
    {
        var errors = new List<ApplicationError>();

        if (receivableAccount.Type != LedgerAccountType.Asset)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.AccountsReceivableAccountId),
                "Accounts receivable ledger account must be an asset account."));
        }

        if (!receivableAccount.IsPostingAccount)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.AccountsReceivableAccountId),
                "Accounts receivable ledger account must be a posting account."));
        }

        if (receivableAccount.Status != LedgerAccountStatus.Active)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.AccountsReceivableAccountId),
                "Accounts receivable ledger account must be active."));
        }

        return errors;
    }

    private static ClientAccountingProfileResult ToResult(ClientAccountingProfile profile)
    {
        return new ClientAccountingProfileResult(
            profile.ClientId.Value,
            profile.AccountsReceivableAccountId.Value,
            profile.DefaultCurrencyCode,
            profile.CloudCustomerId,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc);
    }
}
