namespace SafarSuite.ControlDesk.Application.Common.Results;

public sealed record ApplicationError(string Code, string Message, string? Target = null)
{
    public static ApplicationError Validation(string target, string message)
    {
        return new ApplicationError("validation", message, target);
    }

    public static ApplicationError Conflict(string target, string message)
    {
        return new ApplicationError("conflict", message, target);
    }

    public static ApplicationError NotFound(string target, string message)
    {
        return new ApplicationError("not_found", message, target);
    }

    public static ApplicationError Forbidden(string target, string message)
    {
        return new ApplicationError("forbidden", message, target);
    }

    public static ApplicationError Unexpected(string message)
    {
        return new ApplicationError("unexpected", message);
    }

    public static ApplicationError ServiceUnavailable(string message, string? target = null)
    {
        return new ApplicationError("service_unavailable", message, target);
    }
}
