using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.PublishProductCatalogRevision;

public sealed class PublishProductCatalogRevisionHandler
{
    private readonly IProductModuleCatalog _catalog;
    private readonly IProductCatalogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;

    public PublishProductCatalogRevisionHandler(
        IProductModuleCatalog catalog,
        IProductCatalogRepository repository,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock)
    {
        _catalog = catalog;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task<Result<ProductCatalogSnapshotResult>> HandleAsync(
        PublishProductCatalogRevisionCommand command,
        CancellationToken cancellationToken = default)
    {
        var requestedBy = command.RequestedBy?.Trim();

        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            return Result<ProductCatalogSnapshotResult>.Failure(ApplicationError.Validation(
                nameof(command.RequestedBy),
                "Publisher is required before publishing a product catalog revision."));
        }

        if (requestedBy.Length > 160)
        {
            return Result<ProductCatalogSnapshotResult>.Failure(ApplicationError.Validation(
                nameof(command.RequestedBy),
                "Publisher cannot exceed 160 characters."));
        }

        try
        {
            await _catalog.GetPublishedRevisionAsync(cancellationToken);

            var published = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var latest = await _repository.GetLatestPublishedForUpdateAsync(token)
                        ?? throw new InvalidOperationException("Published product catalog was not found.");
                    var draft = await _repository.GetDraftAsync(token)
                        ?? throw new InvalidOperationException(
                            "No product catalog draft is ready to publish.");

                    if (draft.BaseRevisionId != latest.Id
                        || draft.BaseRevisionNumber != latest.RevisionNumber)
                    {
                        throw new InvalidOperationException(
                            "The product catalog draft is based on an older revision. Refresh and save it again before publishing.");
                    }

                    var next = ProductCatalogRevision.Publish(
                        ProductCatalogRevisionId.Create(_idGenerator.NewGuid()),
                        latest.RevisionNumber + 1,
                        latest.Id,
                        draft.Definition,
                        draft.ChangeReason,
                        requestedBy,
                        _clock.UtcNow);

                    await _repository.AddPublishedAsync(next, token);
                    await _repository.DeleteDraftAsync(token);

                    return next;
                },
                cancellationToken);

            return Result<ProductCatalogSnapshotResult>.Success(
                ProductCatalogSnapshotResultMapper.FromPublished(published));
        }
        catch (ArgumentException exception)
        {
            return Result<ProductCatalogSnapshotResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<ProductCatalogSnapshotResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }
}

public sealed record PublishProductCatalogRevisionCommand(string RequestedBy);
