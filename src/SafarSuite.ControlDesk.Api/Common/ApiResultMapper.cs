using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Common;

namespace SafarSuite.ControlDesk.Api.Common;

internal static class ApiResultMapper
{
    public static IResult ToErrorResult(IReadOnlyCollection<ApplicationError> errors)
    {
        var statusCode = GetStatusCode(errors);
        var response = new ApiErrorResponse(
            statusCode,
            GetTitle(statusCode),
            errors.Select(error => new ApiErrorItem(error.Code, error.Message, error.Target)).ToArray());

        return Results.Json(response, statusCode: statusCode);
    }

    private static int GetStatusCode(IReadOnlyCollection<ApplicationError> errors)
    {
        if (errors.Any(error => error.Code == "validation"))
        {
            return StatusCodes.Status400BadRequest;
        }

        if (errors.Any(error => error.Code == "conflict"))
        {
            return StatusCodes.Status409Conflict;
        }

        if (errors.Any(error => error.Code == "not_found"))
        {
            return StatusCodes.Status404NotFound;
        }

        if (errors.Any(error => error.Code == "forbidden"))
        {
            return StatusCodes.Status403Forbidden;
        }

        if (errors.Any(error => error.Code == "service_unavailable"))
        {
            return StatusCodes.Status503ServiceUnavailable;
        }

        return StatusCodes.Status500InternalServerError;
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Request validation failed.",
            StatusCodes.Status403Forbidden => "The operation is not permitted.",
            StatusCodes.Status404NotFound => "Resource was not found.",
            StatusCodes.Status409Conflict => "Request conflicts with existing data.",
            StatusCodes.Status503ServiceUnavailable => "External service is unavailable.",
            _ => "Unexpected server error."
        };
    }
}
