using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.CreateSurveyJob;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.CreateSurveyJobBillingDraft;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.GetSurveyJobEntry;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.UpdateSurveyJobDocuments;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.UpdateSurveyJobInvoiceLines;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.UpdateSurveyJob;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.SurveyValuation;

namespace SafarSuite.ControlDesk.Api.Modules.SurveyValuation;

public static class SurveyJobEntryEndpoints
{
    public static IEndpointRouteBuilder MapSurveyJobEntryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/survey-valuation/jobs")
            .WithTags("Survey Valuation");

        group.MapPost("/", CreateAsync);
        group.MapGet("/{surveyJobId:guid}", GetByIdAsync);
        group.MapGet("/by-number/{surveyJobNumber}", GetByNumberAsync);
        group.MapPut("/{surveyJobId:guid}", UpdateAsync);
        group.MapPut("/{surveyJobId:guid}/documents", UpdateDocumentsAsync);
        group.MapPut("/{surveyJobId:guid}/invoice-lines", UpdateInvoiceLinesAsync);
        group.MapPost("/{surveyJobId:guid}/billing-draft", CreateBillingDraftAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateSurveyJobRequest request,
        CreateSurveyJobHandler handler,
        CancellationToken cancellationToken)
    {
        var command = SurveyJobEntryApiMapper.ToCommand(request);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new CreateSurveyJobResponse(
            result.Value.SurveyJobId,
            result.Value.SurveyJobNumber,
            result.Value.Status);

        return Results.Created($"/api/v1/survey-valuation/jobs/{response.SurveyJobId}", response);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid surveyJobId,
        GetSurveyJobEntryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetSurveyJobEntryQuery(SurveyJobId: surveyJobId),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(SurveyJobEntryApiMapper.ToResponse(result.Value));
    }

    private static async Task<IResult> GetByNumberAsync(
        string surveyJobNumber,
        GetSurveyJobEntryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetSurveyJobEntryQuery(SurveyJobNumber: surveyJobNumber),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(SurveyJobEntryApiMapper.ToResponse(result.Value));
    }

    private static async Task<IResult> UpdateAsync(
        Guid surveyJobId,
        UpdateSurveyJobRequest request,
        UpdateSurveyJobHandler handler,
        CancellationToken cancellationToken)
    {
        var command = SurveyJobEntryApiMapper.ToCommand(surveyJobId, request);
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(SurveyJobEntryApiMapper.ToResponse(result.Value));
    }

    private static async Task<IResult> UpdateDocumentsAsync(
        Guid surveyJobId,
        UpdateSurveyJobDocumentsRequest request,
        UpdateSurveyJobDocumentsHandler handler,
        CancellationToken cancellationToken)
    {
        var command = SurveyJobEntryApiMapper.ToCommand(surveyJobId, request);
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(SurveyJobEntryApiMapper.ToResponse(result.Value));
    }

    private static async Task<IResult> UpdateInvoiceLinesAsync(
        Guid surveyJobId,
        UpdateSurveyJobInvoiceLinesRequest request,
        UpdateSurveyJobInvoiceLinesHandler handler,
        CancellationToken cancellationToken)
    {
        var command = SurveyJobEntryApiMapper.ToCommand(surveyJobId, request);
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(SurveyJobEntryApiMapper.ToResponse(result.Value));
    }

    private static async Task<IResult> CreateBillingDraftAsync(
        Guid surveyJobId,
        CreateSurveyJobBillingDraftRequest request,
        CreateSurveyJobBillingDraftHandler handler,
        CancellationToken cancellationToken)
    {
        var command = SurveyJobEntryApiMapper.ToCommand(surveyJobId, request);
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Created(
                $"/api/v1/billing/invoices/{result.Value.InvoiceId}",
                SurveyJobEntryApiMapper.ToResponse(result.Value));
    }
}
