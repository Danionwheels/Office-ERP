namespace SafarSuite.ControlDesk.Application.Modules.Payments.UpdateProviderBankDetails;

public sealed record UpdateProviderBankDetailsCommand(
    string BankName,
    string AccountTitle,
    string AccountNumber,
    string Iban,
    string BranchOrRoutingInfo);
