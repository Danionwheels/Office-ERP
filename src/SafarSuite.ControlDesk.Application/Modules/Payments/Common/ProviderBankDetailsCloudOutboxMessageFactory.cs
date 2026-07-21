using System.Text.Json;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.Common;

public sealed class ProviderBankDetailsCloudOutboxMessageFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IIdGenerator _idGenerator;

    public ProviderBankDetailsCloudOutboxMessageFactory(IIdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
    }

    public CloudOutboxMessage CreateUpdated(ProviderBankDetails details)
    {
        var payload = new ProviderBankDetailsUpdatedCloudPayload(
            details.BankName,
            details.AccountTitle,
            details.AccountNumber,
            details.Iban,
            details.BranchOrRoutingInfo,
            details.UpdatedAtUtc);

        return CloudOutboxMessage.CreateSystem(
            CloudOutboxMessageId.Create(_idGenerator.NewGuid()),
            "ProviderBankDetailsUpdated",
            "ProviderBankDetails",
            "provider",
            JsonSerializer.Serialize(payload, JsonOptions),
            details.UpdatedAtUtc);
    }

    private sealed record ProviderBankDetailsUpdatedCloudPayload(
        string BankName,
        string AccountTitle,
        string AccountNumber,
        string Iban,
        string BranchOrRoutingInfo,
        DateTimeOffset UpdatedAtUtc);
}
