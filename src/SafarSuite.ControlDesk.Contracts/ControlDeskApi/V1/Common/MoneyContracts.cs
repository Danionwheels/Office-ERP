namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Common;

public sealed record MoneyRequest(decimal Amount, string CurrencyCode);

public sealed record MoneyResponse(decimal Amount, string CurrencyCode);
