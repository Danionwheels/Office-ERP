namespace SafarSuite.ControlDesk.Application.Modules.Payments.ApplyClientCredit;

public sealed record ApplyClientCreditResult(
    Guid CreditApplicationId,
    Guid ClientId,
    Guid InvoiceId,
    string InvoiceNumber,
    string InvoiceStatus,
    string Reference,
    decimal Amount,
    decimal InvoiceBalanceBefore,
    decimal InvoiceBalanceAfter,
    decimal AvailableCreditBefore,
    decimal AvailableCreditAfter,
    decimal ClientBalanceBefore,
    decimal ClientBalanceAfter,
    string CurrencyCode,
    DateOnly AppliedOn,
    string CreditApplicationStatus);
