using SafarSuite.ControlDesk.Application.Common.Results;

namespace SafarSuite.ControlDesk.AccountingSmoke;

internal static class SmokeAssertions
{
    public static T RequireSuccess<T>(Result<T> result, string step)
    {
        if (result.IsSuccess)
        {
            return result.Value;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(error => $"{error.Code}:{error.Target}:{error.Message}"));

        throw new SmokeFailureException($"{step} failed: {errors}");
    }

    public static ApplicationError RequireFailure<T>(
        Result<T> result,
        string step,
        string? expectedTarget = null,
        string? expectedMessageFragment = null)
    {
        if (result.IsSuccess)
        {
            throw new SmokeFailureException($"{step} should have failed.");
        }

        var error = expectedTarget is null
            ? result.Errors.First()
            : result.Errors.FirstOrDefault(error =>
                string.Equals(error.Target, expectedTarget, StringComparison.OrdinalIgnoreCase))
                ?? throw new SmokeFailureException(
                    $"{step} did not return an error for target {expectedTarget}.");

        if (!string.IsNullOrWhiteSpace(expectedMessageFragment)
            && !error.Message.Contains(expectedMessageFragment, StringComparison.OrdinalIgnoreCase))
        {
            throw new SmokeFailureException(
                $"{step} expected error containing '{expectedMessageFragment}' but was '{error.Message}'.");
        }

        return error;
    }

    public static void Equal<T>(T expected, T actual, string label)
        where T : IEquatable<T>
    {
        if (!expected.Equals(actual))
        {
            throw new SmokeFailureException($"{label} expected {expected} but was {actual}.");
        }
    }

    public static void Money(decimal expected, decimal actual, string label)
    {
        if (decimal.Round(expected, 2) != decimal.Round(actual, 2))
        {
            throw new SmokeFailureException($"{label} expected {expected:0.00} but was {actual:0.00}.");
        }
    }

    public static void True(bool condition, string label)
    {
        if (!condition)
        {
            throw new SmokeFailureException(label);
        }
    }
}
