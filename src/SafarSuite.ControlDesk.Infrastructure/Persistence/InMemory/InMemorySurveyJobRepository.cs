using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.Ports;
using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemorySurveyJobRepository : ISurveyJobRepository
{
    private readonly ConcurrentDictionary<Guid, SurveyJob> _jobsById = new();

    public Task AddAsync(SurveyJob surveyJob, CancellationToken cancellationToken = default)
    {
        _jobsById.TryAdd(surveyJob.Id.Value, surveyJob);

        return Task.CompletedTask;
    }

    public Task<SurveyJob?> GetByIdAsync(SurveyJobId id, CancellationToken cancellationToken = default)
    {
        _jobsById.TryGetValue(id.Value, out var surveyJob);

        return Task.FromResult(surveyJob);
    }

    public Task<SurveyJob?> GetByNumberAsync(SurveyJobNumber number, CancellationToken cancellationToken = default)
    {
        var surveyJob = _jobsById.Values.FirstOrDefault(job => job.Number.Equals(number));

        return Task.FromResult(surveyJob);
    }

    public Task<bool> ExistsByNumberAsync(SurveyJobNumber number, CancellationToken cancellationToken = default)
    {
        var exists = _jobsById.Values.Any(job => job.Number.Equals(number));

        return Task.FromResult(exists);
    }
}
