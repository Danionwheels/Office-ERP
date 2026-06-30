using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.Ports;

public interface ISurveyJobRepository
{
    Task AddAsync(SurveyJob surveyJob, CancellationToken cancellationToken = default);

    Task<SurveyJob?> GetByIdAsync(SurveyJobId id, CancellationToken cancellationToken = default);

    Task<SurveyJob?> GetByNumberAsync(SurveyJobNumber number, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNumberAsync(SurveyJobNumber number, CancellationToken cancellationToken = default);
}
