using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewJournalVoucherNumber;

public sealed class PreviewJournalVoucherNumberHandler
{
    private readonly JournalVoucherNumberService _voucherNumbers;

    public PreviewJournalVoucherNumberHandler(JournalVoucherNumberService voucherNumbers)
    {
        _voucherNumbers = voucherNumbers;
    }

    public async Task<Result<PreviewJournalVoucherNumberResult>> HandleAsync(
        PreviewJournalVoucherNumberQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.EntryDate == default)
        {
            return Result<PreviewJournalVoucherNumberResult>.Failure(ApplicationError.Validation(
                nameof(query.EntryDate),
                "Voucher entry date is required."));
        }

        if (!Enum.TryParse<JournalSourceType>(query.SourceType, ignoreCase: true, out var sourceType))
        {
            return Result<PreviewJournalVoucherNumberResult>.Failure(ApplicationError.Validation(
                nameof(query.SourceType),
                $"Journal source type '{query.SourceType}' is not supported."));
        }

        var preview = await _voucherNumbers.PreviewNextAsync(sourceType, query.EntryDate, cancellationToken);

        return Result<PreviewJournalVoucherNumberResult>.Success(new PreviewJournalVoucherNumberResult(
            preview.SourceType,
            preview.EntryDate,
            preview.Prefix,
            preview.SequenceYear,
            preview.NextSequence,
            preview.NumberPaddingWidth,
            preview.Reference));
    }
}
