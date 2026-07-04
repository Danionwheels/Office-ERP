using System.Net.Http.Json;
using System.Text.Json;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Contracts;

var options = SmokeOptions.Parse(args);
if (options.ShowHelp)
{
    Console.WriteLine(SmokeOptions.HelpText);
    return 0;
}

var checks = new List<string>();
using var controlDesk = new HttpClient
{
    BaseAddress = options.ControlDeskUrl
};

ProductAccessCatalogResponse? originalCatalog = null;
var restored = false;

try
{
    originalCatalog = await GetCatalogAsync(controlDesk);
    Require(originalCatalog.ModuleGroups.Count > 0, "Control Desk should return at least one product module group.");
    Require(originalCatalog.Resources.Count > 0, "Control Desk should return at least one product resource.");
    checks.Add($"read current catalog ({originalCatalog.ModuleGroups.Count} groups, {originalCatalog.Resources.Count} resources)");

    var smokeCatalog = BuildSmokeCatalog(originalCatalog, options);
    var savedCatalog = await SaveCatalogAsync(controlDesk, smokeCatalog, options.RequestedBy);
    Require(savedCatalog.ModuleGroups.Any(group => EqualsId(group.GroupId, options.GroupId)), "Saved catalog should include the smoke module group.");
    Require(savedCatalog.Resources.Any(resource => EqualsId(resource.ResourceId, options.ResourceId)), "Saved catalog should include the smoke resource.");
    Require(
        savedCatalog.Resources
            .First(resource => EqualsId(resource.ResourceId, options.ResourceId))
            .ResolvedModuleCodes
            .Any(moduleCode => string.Equals(moduleCode, "payroll", StringComparison.OrdinalIgnoreCase)),
        "Saved smoke resource should resolve Payroll from its group.");
    checks.Add("saved temporary owner catalog group/resource and verified resolver output");

    var readBackCatalog = await GetCatalogAsync(controlDesk);
    Require(readBackCatalog.ModuleGroups.Any(group => EqualsId(group.GroupId, options.GroupId)), "Readback catalog should include the smoke module group.");
    Require(readBackCatalog.Resources.Any(resource => EqualsId(resource.ResourceId, options.ResourceId)), "Readback catalog should include the smoke resource.");
    checks.Add("readback returned the persisted smoke catalog");

    if (!options.SkipRestore)
    {
        await SaveCatalogAsync(controlDesk, originalCatalog, $"{options.RequestedBy} restore");
        restored = true;
        var restoredCatalog = await GetCatalogAsync(controlDesk);
        Require(
            CatalogHasSameShape(originalCatalog, restoredCatalog),
            "Restored catalog should match the original group/resource shape.");
        checks.Add("restored original catalog after save/read proof");
    }

    if (options.PublishActivationRequestId is { } activationRequestId)
    {
        var publishResponse = await PublishCatalogAsync(controlDesk, activationRequestId, options);
        Require(publishResponse.CommandType == "SetProductAccessCatalog", "Publish response should issue SetProductAccessCatalog.");
        Require(publishResponse.AccessCatalog.ModuleGroups.Count > 0, "Published command should include module groups.");
        Require(publishResponse.AccessCatalog.Resources.Count > 0, "Published command should include resources.");
        checks.Add($"published product-kernel command {publishResponse.CommandId:D}");

        if (options.LocalServerUrl is { } localServerUrl)
        {
            await ImportProductKernelCommandAsync(localServerUrl, publishResponse);
            checks.Add("imported signed product-kernel command into app LocalServer");

            var state = await ReadLocalProductKernelStateAsync(localServerUrl);
            Require(state.GroupCount == publishResponse.AccessCatalog.ModuleGroups.Count, "LocalServer state should have the published group count.");
            Require(state.ResourceCount == publishResponse.AccessCatalog.Resources.Count, "LocalServer state should have the published resource count.");
            checks.Add($"verified LocalServer catalog readback ({state.GroupCount} groups, {state.ResourceCount} resources)");
        }
    }
}
finally
{
    if (originalCatalog is not null && !restored && !options.SkipRestore)
    {
        try
        {
            await SaveCatalogAsync(controlDesk, originalCatalog, $"{options.RequestedBy} restore");
            Console.WriteLine("Restored original product access catalog after smoke cleanup.");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Failed to restore original product access catalog: {exception.Message}");
        }
    }
}

Console.WriteLine($"Product access catalog smoke passed {checks.Count} checks:");
foreach (var check in checks)
{
    Console.WriteLine($"- {check}");
}

return 0;

static async Task<ProductAccessCatalogResponse> GetCatalogAsync(HttpClient http)
{
    using var response = await http.GetAsync("/api/v1/contracts/product-access-catalog");
    await EnsureSuccessAsync(response, "read product access catalog");

    return await response.Content.ReadFromJsonAsync<ProductAccessCatalogResponse>()
        ?? throw new InvalidOperationException("Control Desk returned an empty product access catalog response.");
}

static async Task<ProductAccessCatalogResponse> SaveCatalogAsync(
    HttpClient http,
    ProductAccessCatalogResponse catalog,
    string requestedBy)
{
    var request = new SaveProductAccessCatalogRequest(
        catalog.ModuleGroups.Select(group => new SaveProductModuleGroupRequest(
                group.GroupId,
                group.DisplayName,
                group.AccessKind,
                group.ModuleCodes.ToArray()))
            .ToArray(),
        catalog.Resources.Select(resource => new SaveProductResourceRequest(
                resource.ResourceId,
                resource.DisplayName,
                resource.AccessKind,
                resource.RequiredGroupIds.ToArray(),
                resource.RequiredModuleCodes.ToArray()))
            .ToArray(),
        requestedBy);

    using var response = await http.PutAsJsonAsync("/api/v1/contracts/product-access-catalog", request);
    await EnsureSuccessAsync(response, "save product access catalog");

    return await response.Content.ReadFromJsonAsync<ProductAccessCatalogResponse>()
        ?? throw new InvalidOperationException("Control Desk returned an empty saved catalog response.");
}

static ProductAccessCatalogResponse BuildSmokeCatalog(
    ProductAccessCatalogResponse current,
    SmokeOptions options)
{
    var groups = current.ModuleGroups
        .Where(group => !EqualsId(group.GroupId, options.GroupId))
        .Append(new ProductModuleGroupResponse(
            options.GroupId,
            options.GroupDisplayName,
            "PaidModule",
            new[] { "payroll", "payroll-reports" }))
        .ToArray();

    var resources = current.Resources
        .Where(resource => !EqualsId(resource.ResourceId, options.ResourceId))
        .Append(new ProductResourceResponse(
            options.ResourceId,
            options.ResourceDisplayName,
            "PaidModule",
            new[] { options.GroupId },
            Array.Empty<string>(),
            Array.Empty<string>()))
        .ToArray();

    return new ProductAccessCatalogResponse(groups, resources);
}

static async Task<PublishProductAccessCatalogCommandResponse> PublishCatalogAsync(
    HttpClient controlDesk,
    Guid activationRequestId,
    SmokeOptions options)
{
    var request = new PublishProductAccessCatalogCommandRequest(
        activationRequestId,
        options.ExpiresInHours,
        options.RequestedBy);

    using var response = await controlDesk.PostAsJsonAsync(
        "/api/v1/contracts/product-access-catalog/product-kernel-command",
        request);
    await EnsureSuccessAsync(response, "publish product access catalog command");

    return await response.Content.ReadFromJsonAsync<PublishProductAccessCatalogCommandResponse>()
        ?? throw new InvalidOperationException("Control Desk returned an empty publish response.");
}

static async Task ImportProductKernelCommandAsync(
    Uri localServerUrl,
    PublishProductAccessCatalogCommandResponse publishResponse)
{
    using var localServer = new HttpClient
    {
        BaseAddress = localServerUrl
    };
    using var response = await localServer.PostAsJsonAsync(
        "/api/product-kernel/vendor-commands",
        new
        {
            publishResponse.ProductKernelCommand,
            publishResponse.Signature,
            publishResponse.SigningKeyId
        });

    await EnsureSuccessAsync(response, "import product-kernel command into LocalServer");
}

static async Task<(int GroupCount, int ResourceCount)> ReadLocalProductKernelStateAsync(Uri localServerUrl)
{
    using var localServer = new HttpClient
    {
        BaseAddress = localServerUrl
    };
    using var response = await localServer.GetAsync("/api/product-kernel/state");
    await EnsureSuccessAsync(response, "read LocalServer product-kernel state");

    using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    var accessCatalog = document.RootElement.GetProperty("accessCatalog");
    return (
        accessCatalog.GetProperty("moduleGroups").GetArrayLength(),
        accessCatalog.GetProperty("resources").GetArrayLength());
}

static bool CatalogHasSameShape(
    ProductAccessCatalogResponse expected,
    ProductAccessCatalogResponse actual)
{
    return SameSet(
            expected.ModuleGroups.Select(group => group.GroupId),
            actual.ModuleGroups.Select(group => group.GroupId))
        && SameSet(
            expected.Resources.Select(resource => resource.ResourceId),
            actual.Resources.Select(resource => resource.ResourceId));
}

static bool SameSet(IEnumerable<string> left, IEnumerable<string> right)
{
    return new HashSet<string>(left, StringComparer.OrdinalIgnoreCase)
        .SetEquals(right);
}

static bool EqualsId(string left, string right)
{
    return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static async Task EnsureSuccessAsync(HttpResponseMessage response, string action)
{
    if (response.IsSuccessStatusCode)
    {
        return;
    }

    var body = await response.Content.ReadAsStringAsync();
    throw new InvalidOperationException(
        $"Failed to {action}. HTTP {(int)response.StatusCode}: {body}");
}

internal sealed record SmokeOptions(
    Uri ControlDeskUrl,
    string RequestedBy,
    string GroupId,
    string GroupDisplayName,
    string ResourceId,
    string ResourceDisplayName,
    bool SkipRestore,
    Guid? PublishActivationRequestId,
    Uri? LocalServerUrl,
    int ExpiresInHours,
    bool ShowHelp)
{
    public const string HelpText = """
        Usage:
          dotnet run --project tools/SafarSuite.ControlDesk.ProductAccessCatalogSmoke -- [options]

        Options:
          --control-desk-url <url>              Control Desk API base URL. Default: http://localhost:5188
          --requested-by <name>                 Audit actor. Default: ProductAccessCatalogSmoke
          --group-id <id>                       Temporary group id. Default: smoke-payroll
          --resource-id <id>                    Temporary resource id. Default: smoke.payroll.run
          --skip-restore                        Leave the temporary catalog saved after the smoke.
          --publish-activation-request-id <id>  Also publish the current catalog through Control Desk.
          --local-server-url <url>              Import the published command and verify LocalServer readback.
          --expires-in-hours <hours>            Publish command lifetime. Default: 2
          --help                                Show help.
        """;

    public static SmokeOptions Parse(string[] args)
    {
        var controlDeskUrl = new Uri("http://localhost:5188");
        var requestedBy = "ProductAccessCatalogSmoke";
        var groupId = "smoke-payroll";
        var resourceId = "smoke.payroll.run";
        var skipRestore = false;
        Guid? publishActivationRequestId = null;
        Uri? localServerUrl = null;
        var expiresInHours = 2;
        var showHelp = false;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--help":
                case "-h":
                    showHelp = true;
                    break;

                case "--control-desk-url":
                    controlDeskUrl = ReadUri(args, ref index, arg);
                    break;

                case "--requested-by":
                    requestedBy = ReadRequired(args, ref index, arg);
                    break;

                case "--group-id":
                    groupId = ReadRequired(args, ref index, arg);
                    break;

                case "--resource-id":
                    resourceId = ReadRequired(args, ref index, arg);
                    break;

                case "--skip-restore":
                    skipRestore = true;
                    break;

                case "--publish-activation-request-id":
                    publishActivationRequestId = Guid.Parse(ReadRequired(args, ref index, arg));
                    break;

                case "--local-server-url":
                    localServerUrl = ReadUri(args, ref index, arg);
                    break;

                case "--expires-in-hours":
                    expiresInHours = int.Parse(ReadRequired(args, ref index, arg));
                    break;

                default:
                    throw new InvalidOperationException($"Unknown option '{arg}'.{Environment.NewLine}{HelpText}");
            }
        }

        if (expiresInHours is < 1 or > 168)
        {
            throw new InvalidOperationException("--expires-in-hours must be between 1 and 168.");
        }

        return new SmokeOptions(
            controlDeskUrl,
            requestedBy,
            groupId,
            "Smoke Payroll",
            resourceId,
            "Smoke Payroll Run",
            skipRestore,
            publishActivationRequestId,
            localServerUrl,
            expiresInHours,
            showHelp);
    }

    private static string ReadRequired(string[] args, ref int index, string option)
    {
        if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new InvalidOperationException($"{option} requires a value.");
        }

        return args[index].Trim();
    }

    private static Uri ReadUri(string[] args, ref int index, string option)
    {
        var value = ReadRequired(args, ref index, option);
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri
            : throw new InvalidOperationException($"{option} must be an absolute URL.");
    }
}
