namespace SafarSuite.ConnectedAcceptance;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Any(argument => argument is "--help" or "-h"))
        {
            Console.WriteLine(ConnectedAcceptanceOptions.Usage);

            return 0;
        }

        try
        {
            var options = ConnectedAcceptanceOptions.Parse(args);
            var runner = new ConnectedAcceptanceRunner(options);
            var result = await runner.RunAsync();

            Console.WriteLine(
                $"Connected acceptance passed (run {result.RunId}, client {result.ClientId}, " +
                $"entitlement v{result.EntitlementVersion}, reconciliation {result.ReconciliationState}).");
            Console.WriteLine($"Evidence: {result.EvidencePath}");
            Console.WriteLine($"SHA-256: {result.EvidenceSha256}");

            return 0;
        }
        catch (ConnectedAcceptanceFailureException exception)
        {
            Console.Error.WriteLine("Connected acceptance failed.");
            Console.Error.WriteLine(exception.Message);

            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("Connected acceptance crashed.");
            Console.Error.WriteLine($"{exception.GetType().Name}: {exception.Message}");

            return 1;
        }
    }
}
