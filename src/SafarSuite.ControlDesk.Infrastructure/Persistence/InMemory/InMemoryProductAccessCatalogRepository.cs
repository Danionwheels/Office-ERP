using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryProductAccessCatalogRepository : IProductCatalogRepository
{
    private readonly object _gate = new();
    private readonly List<ProductCatalogRevision> _published = [];
    private ProductCatalogDraft? _draft;

    public Task<ProductCatalogRevision?> GetLatestPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult(_published
                .OrderByDescending(revision => revision.RevisionNumber)
                .FirstOrDefault());
        }
    }

    public Task<ProductCatalogRevision?> GetLatestPublishedForUpdateAsync(
        CancellationToken cancellationToken = default)
    {
        return GetLatestPublishedAsync(cancellationToken);
    }

    public Task<ProductCatalogDraft?> GetDraftAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult(_draft);
        }
    }

    public Task<IReadOnlyCollection<ProductCatalogRevision>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult<IReadOnlyCollection<ProductCatalogRevision>>(
                _published
                    .OrderByDescending(revision => revision.RevisionNumber)
                    .ToArray());
        }
    }

    public Task AddPublishedAsync(
        ProductCatalogRevision revision,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_published.Any(existing =>
                    existing.Id == revision.Id
                    || existing.RevisionNumber == revision.RevisionNumber
                    || (revision.SupersedesRevisionId is not null
                        && existing.SupersedesRevisionId == revision.SupersedesRevisionId)))
            {
                throw new InvalidOperationException("Product catalog revision conflicts with published history.");
            }

            _published.Add(revision);
        }

        return Task.CompletedTask;
    }

    public Task SaveDraftAsync(
        ProductCatalogDraft draft,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _draft = draft;
        }

        return Task.CompletedTask;
    }

    public Task DeleteDraftAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _draft = null;
        }

        return Task.CompletedTask;
    }
}
