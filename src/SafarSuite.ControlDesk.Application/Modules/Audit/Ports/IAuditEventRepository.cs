using SafarSuite.ControlDesk.Domain.Modules.Audit;

namespace SafarSuite.ControlDesk.Application.Modules.Audit.Ports;

public interface IAuditEventRepository
{
    Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
