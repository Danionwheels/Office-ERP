namespace SafarSuite.ControlDesk.Application.Common.Abstractions;

public interface ICurrentUser
{
    string? UserId { get; }

    string? DisplayName { get; }

    bool IsAuthenticated { get; }
}
