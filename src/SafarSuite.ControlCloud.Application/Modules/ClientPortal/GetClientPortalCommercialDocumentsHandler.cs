using System.Globalization;
using System.Text;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class GetClientPortalCommercialDocumentsHandler
{
    private readonly IControlCloudClientCommercialProjectionRepository _projections;

    public GetClientPortalCommercialDocumentsHandler(
        IControlCloudClientCommercialProjectionRepository projections)
    {
        _projections = projections;
    }

    public async Task<GetClientPortalCommercialDocumentsResult> HandleAsync(
        GetClientPortalCommercialDocumentsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.ClientId == Guid.Empty)
        {
            return GetClientPortalCommercialDocumentsResult.Failure(
                "ClientIdRequired",
                "Client id is required before listing commercial documents.");
        }

        if (!ControlCloudCommercialDocumentTypes.TryNormalize(query.DocumentType, out var documentType))
        {
            return GetClientPortalCommercialDocumentsResult.Failure(
                "CommercialDocumentTypeInvalid",
                $"Document type must be one of: {string.Join(", ", ControlCloudCommercialDocumentTypes.All)}.");
        }

        if (query.Take is < 1 or > 100)
        {
            return GetClientPortalCommercialDocumentsResult.Failure(
                "CommercialDocumentPageSizeInvalid",
                "Page size must be between 1 and 100.");
        }

        if (!TryDecodeCursor(query.Cursor, out var beforeDate, out var beforeDocumentId))
        {
            return GetClientPortalCommercialDocumentsResult.Failure(
                "CommercialDocumentCursorInvalid",
                "Commercial document cursor is invalid or malformed.");
        }

        var documents = await _projections.ListDocumentsAsync(
            query.ClientId,
            documentType,
            beforeDate,
            beforeDocumentId,
            query.Take + 1,
            cancellationToken);
        var hasMore = documents.Count > query.Take;
        var items = documents.Take(query.Take).ToArray();
        var nextCursor = hasMore && items.Length > 0
            ? EncodeCursor(items[^1])
            : null;

        return GetClientPortalCommercialDocumentsResult.Success(
            query.ClientId,
            documentType,
            query.Take,
            hasMore,
            nextCursor,
            items);
    }

    private static string EncodeCursor(ControlCloudCommercialDocumentProjection document)
    {
        var value = $"{document.DocumentDate:yyyyMMdd}|{document.DocumentId:N}";

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static bool TryDecodeCursor(
        string? cursor,
        out DateOnly? beforeDate,
        out Guid? beforeDocumentId)
    {
        beforeDate = null;
        beforeDocumentId = null;

        if (string.IsNullOrWhiteSpace(cursor))
        {
            return true;
        }

        try
        {
            var normalized = cursor.Trim().Replace('-', '+').Replace('_', '/');
            normalized = normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '=');
            var value = Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
            var parts = value.Split('|', StringSplitOptions.TrimEntries);

            if (parts.Length != 2
                || !DateOnly.TryParseExact(
                    parts[0],
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate)
                || !Guid.TryParseExact(parts[1], "N", out var parsedDocumentId))
            {
                return false;
            }

            beforeDate = parsedDate;
            beforeDocumentId = parsedDocumentId;

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed record GetClientPortalCommercialDocumentsResult(
    bool IsSuccess,
    string? FailureCode,
    string? Detail,
    Guid ClientId,
    string? DocumentType,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    IReadOnlyCollection<ControlCloudCommercialDocumentProjection> Items)
{
    public static GetClientPortalCommercialDocumentsResult Success(
        Guid clientId,
        string documentType,
        int pageSize,
        bool hasMore,
        string? nextCursor,
        IReadOnlyCollection<ControlCloudCommercialDocumentProjection> items)
    {
        return new GetClientPortalCommercialDocumentsResult(
            true,
            null,
            null,
            clientId,
            documentType,
            pageSize,
            hasMore,
            nextCursor,
            items);
    }

    public static GetClientPortalCommercialDocumentsResult Failure(
        string failureCode,
        string detail)
    {
        return new GetClientPortalCommercialDocumentsResult(
            false,
            failureCode,
            detail,
            Guid.Empty,
            null,
            0,
            false,
            null,
            Array.Empty<ControlCloudCommercialDocumentProjection>());
    }
}
