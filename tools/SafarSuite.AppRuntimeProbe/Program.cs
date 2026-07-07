using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

AppRuntimeProbeOptions options;

try
{
    options = AppRuntimeProbeOptions.Parse(args);
}
catch (ArgumentException exception)
{
    WriteJson(new
    {
        status = "Failed",
        failureCode = "ProbeArgumentsInvalid",
        detail = exception.Message
    });
    Environment.ExitCode = 1;
    return;
}

if (options.ShowHelp)
{
    Console.WriteLine(AppRuntimeProbeOptions.HelpText);
    return;
}

Environment.ExitCode = options.SelfTest
    ? await RunSelfTestAsync()
    : await RunLiveProbeAsync(options);

static async Task<int> RunLiveProbeAsync(
    AppRuntimeProbeOptions options)
{
    if (!Uri.TryCreate(options.GatewayUrl, UriKind.Absolute, out var gatewayUri)
        || (gatewayUri.Scheme != Uri.UriSchemeHttp && gatewayUri.Scheme != Uri.UriSchemeHttps))
    {
        WriteJson(new
        {
            status = "Failed",
            failureCode = "GatewayUrlInvalid",
            detail = "SAFARSUITE_MODULE_GATEWAY_URL or --gateway-url must be an absolute HTTP/HTTPS URL."
        });
        return 1;
    }

    if (string.IsNullOrWhiteSpace(options.ModuleCode))
    {
        WriteJson(new
        {
            status = "Failed",
            failureCode = "ModuleCodeRequired",
            detail = "SAFARSUITE_REQUIRED_MODULE, SAFARSUITE_MODULE_CODE, or --module is required."
        });
        return 1;
    }

    using var httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(20)
    };
    var probe = new AppRuntimeModuleGatewayProbe(httpClient, gatewayUri);
    var result = await probe.ProbeAsync(
        options.InstallationId,
        options.ModuleCode,
        options.AsOfDate,
        options.RequestedBy,
        CancellationToken.None);

    if (!result.IsSuccess)
    {
        WriteJson(new
        {
            status = "Failed",
            result.FailureCode,
            result.Detail,
            gatewayUrl = gatewayUri.ToString(),
            installationId = options.InstallationId,
            moduleCode = options.ModuleCode
        });
        return 1;
    }

    var access = result.Access!;
    var expectedOutcomeMatched = options.ExpectDenied
        ? !access.IsAllowed
        : access.IsAllowed;

    WriteJson(new
    {
        status = expectedOutcomeMatched ? "Passed" : "Unexpected",
        gatewayUrl = gatewayUri.ToString(),
        installationId = options.InstallationId,
        moduleCode = access.ModuleCode,
        access.IsAllowed,
        access.AccessState,
        access.Reason,
        access.EntitlementVersion,
        access.PaidUntil,
        access.WarningStartsAt,
        access.GraceUntil,
        access.OfflineValidUntil,
        access.CheckedAtUtc,
        expected = options.ExpectDenied ? "Denied" : "Allowed"
    });

    return expectedOutcomeMatched ? 0 : 2;
}

static async Task<int> RunSelfTestAsync()
{
    using var httpClient = new HttpClient(new StaticModuleGatewayHandler());
    var probe = new AppRuntimeModuleGatewayProbe(
        httpClient,
        new Uri("https://local-server.test"));
    var allowed = await probe.ProbeAsync(
        "office-main",
        "Billing",
        asOfDate: null,
        requestedBy: "safarsuite-app-runtime-probe:self-test",
        CancellationToken.None);
    var denied = await probe.ProbeAsync(
        "office-main",
        "Reports",
        asOfDate: null,
        requestedBy: "safarsuite-app-runtime-probe:self-test",
        CancellationToken.None);
    var passed = allowed.IsSuccess
        && allowed.Access?.IsAllowed == true
        && allowed.Access.AccessState == "Active"
        && denied.IsSuccess
        && denied.Access?.IsAllowed == false
        && denied.Access.AccessState == "ModuleDisabled";

    WriteJson(new
    {
        status = passed ? "Passed" : "Failed",
        allowedModule = allowed.Access?.ModuleCode,
        allowedState = allowed.Access?.AccessState,
        deniedModule = denied.Access?.ModuleCode,
        deniedState = denied.Access?.AccessState,
        contractFormat = allowed.Access?.FormatVersion
    });

    return passed ? 0 : 1;
}

static void WriteJson<T>(T value)
{
    Console.WriteLine(JsonSerializer.Serialize(
        value,
        new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        }));
}

internal sealed class AppRuntimeModuleGatewayProbe
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly Uri _gatewayBaseUrl;

    public AppRuntimeModuleGatewayProbe(
        HttpClient httpClient,
        Uri gatewayBaseUrl)
    {
        _httpClient = httpClient;
        _gatewayBaseUrl = gatewayBaseUrl;
    }

    public async Task<AppRuntimeModuleGatewayProbeResult> ProbeAsync(
        string? installationId,
        string moduleCode,
        DateOnly? asOfDate,
        string requestedBy,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = string.IsNullOrWhiteSpace(installationId)
                ? await GetFromBootstrapAsync(moduleCode, asOfDate, requestedBy, cancellationToken)
                : await PostWithInstallationAsync(installationId, moduleCode, asOfDate, requestedBy, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(cancellationToken);

                return AppRuntimeModuleGatewayProbeResult.Failure(
                    "ModuleGatewayRequestFailed",
                    $"Local module gateway returned {(int)response.StatusCode} {response.ReasonPhrase}: {detail}");
            }

            var access = await response.Content.ReadFromJsonAsync<LocalServerModuleAccessResponse>(
                JsonOptions,
                cancellationToken);

            return access is null
                ? AppRuntimeModuleGatewayProbeResult.Failure(
                    "ModuleGatewayResponseInvalid",
                    "Local module gateway response was empty.")
                : AppRuntimeModuleGatewayProbeResult.Success(access);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return AppRuntimeModuleGatewayProbeResult.Failure(
                "ModuleGatewayTimeout",
                exception.Message);
        }
        catch (HttpRequestException exception)
        {
            return AppRuntimeModuleGatewayProbeResult.Failure(
                "ModuleGatewayUnavailable",
                exception.Message);
        }
        catch (JsonException exception)
        {
            return AppRuntimeModuleGatewayProbeResult.Failure(
                "ModuleGatewayResponseInvalid",
                exception.Message);
        }
    }

    private async Task<HttpResponseMessage> PostWithInstallationAsync(
        string installationId,
        string moduleCode,
        DateOnly? asOfDate,
        string requestedBy,
        CancellationToken cancellationToken)
    {
        var request = new LocalServerModuleAccessRequest(
            installationId.Trim(),
            moduleCode.Trim(),
            asOfDate,
            requestedBy);

        return await _httpClient.PostAsJsonAsync(
            BuildUri("api/v1/local-server/modules/access"),
            request,
            JsonOptions,
            cancellationToken);
    }

    private async Task<HttpResponseMessage> GetFromBootstrapAsync(
        string moduleCode,
        DateOnly? asOfDate,
        string requestedBy,
        CancellationToken cancellationToken)
    {
        var query = new List<string>
        {
            $"requestedBy={Uri.EscapeDataString(requestedBy)}"
        };

        if (asOfDate is not null)
        {
            query.Add($"asOfDate={Uri.EscapeDataString(asOfDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        }

        return await _httpClient.GetAsync(
            BuildUri($"api/v1/local-server/modules/{Uri.EscapeDataString(moduleCode.Trim())}/access?{string.Join("&", query)}"),
            cancellationToken);
    }

    private Uri BuildUri(string relativePath)
    {
        return new Uri(
            $"{_gatewayBaseUrl.ToString().TrimEnd('/')}/{relativePath.TrimStart('/')}");
    }
}

internal sealed record AppRuntimeModuleGatewayProbeResult(
    bool IsSuccess,
    LocalServerModuleAccessResponse? Access,
    string? FailureCode,
    string? Detail)
{
    public static AppRuntimeModuleGatewayProbeResult Success(
        LocalServerModuleAccessResponse access) => new(
        IsSuccess: true,
        access,
        FailureCode: null,
        Detail: null);

    public static AppRuntimeModuleGatewayProbeResult Failure(
        string failureCode,
        string detail) => new(
        IsSuccess: false,
        Access: null,
        failureCode,
        detail);
}

internal sealed record AppRuntimeProbeOptions(
    bool SelfTest,
    bool ShowHelp,
    string GatewayUrl,
    string? InstallationId,
    string ModuleCode,
    DateOnly? AsOfDate,
    string RequestedBy,
    bool ExpectDenied)
{
    public const string HelpText =
        """
        SafarSuite app runtime module-gateway probe

        Options:
          --self-test                  Run without a Local Server API using a fake gateway.
          --gateway-url <url>          Local module gateway base URL. Defaults to SAFARSUITE_MODULE_GATEWAY_URL or http://localhost:51046.
          --installation-id <id>       Installation id. Defaults to SAFARSUITE_INSTALLATION_ID; omitted uses the bootstrap-based GET route.
          --module <code>              Required module. Defaults to SAFARSUITE_REQUIRED_MODULE, SAFARSUITE_MODULE_CODE, or BILLING.
          --as-of-date <yyyy-MM-dd>    Optional access evaluation date.
          --requested-by <actor>       Probe actor label.
          --expect-denied              Exit 0 only when the gateway denies the module.
        """;

    public static AppRuntimeProbeOptions Parse(string[] args)
    {
        var selfTest = false;
        var showHelp = false;
        var gatewayUrl = GetEnvironmentValue("SAFARSUITE_MODULE_GATEWAY_URL")
            ?? "http://localhost:51046";
        var installationId = GetEnvironmentValue("SAFARSUITE_INSTALLATION_ID");
        var moduleCode = GetEnvironmentValue("SAFARSUITE_REQUIRED_MODULE")
            ?? GetEnvironmentValue("SAFARSUITE_MODULE_CODE")
            ?? "BILLING";
        DateOnly? asOfDate = null;
        var requestedBy = GetEnvironmentValue("SAFARSUITE_RUNTIME_PROBE_REQUESTED_BY")
            ?? "safarsuite-app-runtime-probe";
        var expectDenied = false;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];

            switch (argument)
            {
                case "--self-test":
                    selfTest = true;
                    break;
                case "--help":
                case "-h":
                    showHelp = true;
                    break;
                case "--gateway-url":
                    gatewayUrl = ReadValue(args, ref index, argument);
                    break;
                case "--installation-id":
                    installationId = ReadValue(args, ref index, argument);
                    break;
                case "--module":
                    moduleCode = ReadValue(args, ref index, argument);
                    break;
                case "--as-of-date":
                    if (!DateOnly.TryParse(ReadValue(args, ref index, argument), out var parsedDate))
                    {
                        throw new ArgumentException("--as-of-date must use yyyy-MM-dd format.");
                    }

                    asOfDate = parsedDate;
                    break;
                case "--requested-by":
                    requestedBy = ReadValue(args, ref index, argument);
                    break;
                case "--expect-denied":
                    expectDenied = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{argument}'. Use --help for usage.");
            }
        }

        return new AppRuntimeProbeOptions(
            selfTest,
            showHelp,
            gatewayUrl,
            installationId,
            moduleCode,
            asOfDate,
            requestedBy,
            expectDenied);
    }

    private static string ReadValue(
        string[] args,
        ref int index,
        string optionName)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"{optionName} requires a value.");
        }

        index++;
        return args[index].Trim();
    }

    private static string? GetEnvironmentValue(string name)
    {
        var value = Environment.GetEnvironmentVariable(name)?.Trim();

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

internal sealed class StaticModuleGatewayHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Post
            || request.RequestUri is null
            || !request.RequestUri.AbsolutePath.EndsWith(
                "/api/v1/local-server/modules/access",
                StringComparison.Ordinal))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        var accessRequest = await request.Content!.ReadFromJsonAsync<LocalServerModuleAccessRequest>(
            cancellationToken);
        var isAllowed = accessRequest?.ModuleCode.Equals(
            "Billing",
            StringComparison.OrdinalIgnoreCase) == true;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new LocalServerModuleAccessResponse(
                LocalServerModuleGatewayFormat.Version,
                accessRequest?.InstallationId ?? "office-main",
                accessRequest?.ModuleCode ?? "Unknown",
                isAllowed,
                isAllowed ? "Active" : "ModuleDisabled",
                isAllowed
                    ? "Entitlement is active."
                    : "Requested module is not enabled in the cached entitlement.",
                EntitlementVersion: 102,
                PaidUntil: new DateOnly(2026, 9, 30),
                WarningStartsAt: new DateOnly(2026, 9, 23),
                GraceUntil: new DateOnly(2026, 10, 7),
                OfflineValidUntil: new DateOnly(2026, 10, 14),
                CheckedAtUtc: new DateTimeOffset(2026, 8, 1, 10, 4, 0, TimeSpan.Zero)))
        };
    }
}
