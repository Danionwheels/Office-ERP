using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.ProductModules;

public sealed class PersistedProductModuleCatalog : IProductModuleCatalog
{
    private const string BootstrapActor = "Control Desk catalog bootstrap";
    private const string BootstrapReason = "Imported configured product definition as the initial published revision.";

    private readonly ConfiguredProductModuleCatalog _configuredCatalog;
    private readonly IProductCatalogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;

    public PersistedProductModuleCatalog(
        ConfiguredProductModuleCatalog configuredCatalog,
        IProductCatalogRepository repository,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock)
    {
        _configuredCatalog = configuredCatalog;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task<ProductCatalogRevision> GetPublishedRevisionAsync(
        CancellationToken cancellationToken = default)
    {
        var published = await _repository.GetLatestPublishedAsync(cancellationToken);

        if (published is not null)
        {
            return published;
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var current = await _repository.GetLatestPublishedForUpdateAsync(token);

                if (current is not null)
                {
                    return current;
                }

                var definition = await _configuredCatalog.GetDefinitionAsync(token);
                var initial = ProductCatalogRevision.Publish(
                    ProductCatalogRevisionId.Create(_idGenerator.NewGuid()),
                    revisionNumber: 1,
                    supersedesRevisionId: null,
                    definition,
                    BootstrapReason,
                    BootstrapActor,
                    _clock.UtcNow);

                await _repository.AddPublishedAsync(initial, token);

                return initial;
            },
            cancellationToken);
    }

    public async Task<ProductCatalogDraft?> GetDraftAsync(
        CancellationToken cancellationToken = default)
    {
        await GetPublishedRevisionAsync(cancellationToken);
        return await _repository.GetDraftAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductCatalogRevision>> ListPublishedRevisionsAsync(
        CancellationToken cancellationToken = default)
    {
        await GetPublishedRevisionAsync(cancellationToken);
        return await _repository.ListPublishedAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductModuleCatalogItem>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return (await GetPublishedRevisionAsync(cancellationToken)).Definition.Modules;
    }

    public async Task<ProductAccessCatalog> GetAccessCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        return (await GetPublishedRevisionAsync(cancellationToken)).Definition.AccessCatalog;
    }
}
