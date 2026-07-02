namespace SafarSuite.ControlDesk.AccountingSmoke;

internal sealed class SmokeFailureException : Exception
{
    public SmokeFailureException(string message)
        : base(message)
    {
    }
}
