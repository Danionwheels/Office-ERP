using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;

internal static class SurveyJobEntryValidationRules
{
    private const int ReferenceCodeMaxLength = 64;

    public static void ValidateMainFields(
        ICollection<ApplicationError> errors,
        ISurveyJobEntryFields value)
    {
        ValidateRequiredReferenceCode(errors, nameof(value.SurveyTypeCode), value.SurveyTypeCode);
        ValidateOptionalReferenceCodes(errors, value);
        ValidateDates(errors, value);
        ValidateInsuredFields(errors, value);
        ValidatePaymentMode(errors, value);
    }

    public static bool HasInsuredDetails(ISurveyJobEntryFields value)
    {
        return HasValue(value.InsuredName)
            || HasValue(value.InsuredPhone)
            || HasValue(value.InsuredEmail)
            || HasValue(value.InsuredAddress)
            || HasValue(value.InsuredCnic)
            || HasValue(value.ContactPerson)
            || HasValue(value.ContactDesignationCode)
            || HasValue(value.ReferenceNumber)
            || HasValue(value.CcNumber);
    }

    private static void ValidateRequiredReferenceCode(
        ICollection<ApplicationError> errors,
        string target,
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(ApplicationError.Validation(target, "Survey type is required."));
            return;
        }

        if (value.Trim().Length > ReferenceCodeMaxLength)
        {
            errors.Add(ApplicationError.Validation(
                target,
                $"Reference code cannot exceed {ReferenceCodeMaxLength} characters."));
        }
    }

    private static void ValidateOptionalReferenceCodes(
        ICollection<ApplicationError> errors,
        ISurveyJobEntryFields value)
    {
        ValidateReferenceCodeLength(errors, nameof(value.ClientCode), value.ClientCode);
        ValidateReferenceCodeLength(errors, nameof(value.ClientBranchCode), value.ClientBranchCode);
        ValidateReferenceCodeLength(errors, nameof(value.CompanyBranchCode), value.CompanyBranchCode);
        ValidateReferenceCodeLength(errors, nameof(value.BillingBranchCode), value.BillingBranchCode);
        ValidateReferenceCodeLength(errors, nameof(value.ContactDesignationCode), value.ContactDesignationCode);
        ValidateReferenceCodeLength(errors, nameof(value.SurveyorCode), value.SurveyorCode);
        ValidateReferenceCodeLength(errors, nameof(value.SupervisorCode), value.SupervisorCode);
        ValidateReferenceCodeLength(errors, nameof(value.ClaimTypeCode), value.ClaimTypeCode);
        ValidateReferenceCodeLength(errors, nameof(value.RequestSourceCode), value.RequestSourceCode);
        ValidateReferenceCodeLength(errors, nameof(value.AreaCode), value.AreaCode);
        ValidateReferenceCodeLength(errors, nameof(value.AgencyCode), value.AgencyCode);
        ValidateReferenceCodeLength(errors, nameof(value.WorkshopCode), value.WorkshopCode);
    }

    private static void ValidateReferenceCodeLength(
        ICollection<ApplicationError> errors,
        string target,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length <= ReferenceCodeMaxLength)
        {
            return;
        }

        errors.Add(ApplicationError.Validation(
            target,
            $"Reference code cannot exceed {ReferenceCodeMaxLength} characters."));
    }

    private static void ValidateDates(ICollection<ApplicationError> errors, ISurveyJobEntryFields value)
    {
        if (value.IntimationDate == default)
        {
            errors.Add(ApplicationError.Validation(nameof(value.IntimationDate), "Intimation date is required."));
            return;
        }

        if (value.DeliveredDate.HasValue && value.DeliveredDate.Value < value.IntimationDate)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.DeliveredDate),
                "Delivered date cannot be before intimation date."));
        }
    }

    private static void ValidateInsuredFields(
        ICollection<ApplicationError> errors,
        ISurveyJobEntryFields value)
    {
        if (!HasInsuredDetails(value) || !string.IsNullOrWhiteSpace(value.InsuredName))
        {
            return;
        }

        errors.Add(ApplicationError.Validation(
            nameof(value.InsuredName),
            "Insured name is required when insured contact details are provided."));
    }

    private static void ValidatePaymentMode(ICollection<ApplicationError> errors, ISurveyJobEntryFields value)
    {
        if (!Enum.IsDefined(typeof(SurveyPaymentMode), value.PaymentMode))
        {
            errors.Add(ApplicationError.Validation(nameof(value.PaymentMode), "Payment mode is not valid."));
        }
    }

    private static bool HasValue(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }
}
