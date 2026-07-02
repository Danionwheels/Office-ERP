namespace SafarSuite.ControlDesk.AccountingSmoke;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var options = SmokeOptions.Parse(args);
            await using var harness = await SmokeHarness.CreateAsync(options);
            var runner = new AccountingSmokeRunner(harness, options);

            await runner.RunAsync();
            var cloudSummary = runner.PublishedCloudMessageCount > 0
                ? $", published {runner.PublishedCloudMessageCount} cloud messages"
                : "";

            Console.WriteLine($"Accounting smoke passed ({options.Provider}, run {runner.RunId}{cloudSummary}).");

            return 0;
        }
        catch (SmokeFailureException exception)
        {
            Console.Error.WriteLine("Accounting smoke failed.");
            Console.Error.WriteLine(exception.Message);

            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("Accounting smoke crashed.");
            Console.Error.WriteLine($"{exception.GetType().Name}: {exception.Message}");

            return 1;
        }
    }
}
