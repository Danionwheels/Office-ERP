using System.Diagnostics;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.Diagnostics.Ports;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class HttpControlCloudReachabilityProbe(
    HttpClient httpClient,
    IOptions<ControlCloudStatusOptions> options)
    : IControlCloudReachabilityProbe
{
    private static readonly TimeSpan InspectionTimeout = TimeSpan.FromSeconds(3);

    public async Task<ControlCloudReachabilityResult> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(options.Value.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return new ControlCloudReachabilityResult(
                ControlCloudReachabilityStatus.NotConfigured,
                ControlCloudReachabilityCodes.NotConfigured,
                null,
                null);
        }

        using var inspectionCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        inspectionCancellation.CancelAfter(InspectionTimeout);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await httpClient.GetAsync(
                new Uri(baseUri, "/health"),
                HttpCompletionOption.ResponseHeadersRead,
                inspectionCancellation.Token);
            stopwatch.Stop();

            return new ControlCloudReachabilityResult(
                response.IsSuccessStatusCode
                    ? ControlCloudReachabilityStatus.Reachable
                    : ControlCloudReachabilityStatus.Unreachable,
                response.IsSuccessStatusCode
                    ? ControlCloudReachabilityCodes.Reachable
                    : ControlCloudReachabilityCodes.Unreachable,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            return new ControlCloudReachabilityResult(
                ControlCloudReachabilityStatus.TimedOut,
                ControlCloudReachabilityCodes.TimedOut,
                null,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            stopwatch.Stop();

            return new ControlCloudReachabilityResult(
                ControlCloudReachabilityStatus.Unreachable,
                ControlCloudReachabilityCodes.Unreachable,
                null,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception)
        {
            stopwatch.Stop();

            return new ControlCloudReachabilityResult(
                ControlCloudReachabilityStatus.Unreachable,
                ControlCloudReachabilityCodes.Unreachable,
                null,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
