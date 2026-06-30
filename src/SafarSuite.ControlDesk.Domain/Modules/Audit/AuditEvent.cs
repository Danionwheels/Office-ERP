using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Audit;

public sealed class AuditEvent : Entity<AuditEventId>
{
    private AuditEvent(
        AuditEventId id,
        string module,
        string action,
        string subjectType,
        string subjectId,
        string actorId,
        string summary,
        DateTimeOffset occurredAtUtc)
        : base(id)
    {
        Module = module;
        Action = action;
        SubjectType = subjectType;
        SubjectId = subjectId;
        ActorId = actorId;
        Summary = summary;
        OccurredAtUtc = occurredAtUtc;
    }

    public string Module { get; }

    public string Action { get; }

    public string SubjectType { get; }

    public string SubjectId { get; }

    public string ActorId { get; }

    public string Summary { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public static AuditEvent Record(
        AuditEventId id,
        string module,
        string action,
        string subjectType,
        string subjectId,
        string actorId,
        string summary,
        DateTimeOffset occurredAtUtc)
    {
        return new AuditEvent(
            id,
            Require(module, nameof(module)),
            Require(action, nameof(action)),
            Require(subjectType, nameof(subjectType)),
            Require(subjectId, nameof(subjectId)),
            Require(actorId, nameof(actorId)),
            Require(summary, nameof(summary)),
            occurredAtUtc);
    }

    private static string Require(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value.Trim();
    }
}
