using Microsoft.Extensions.Options;

namespace SafarSuite.ControlDesk.Api.Modules.ControlCloud;

public sealed class ControlCloudOutboxWorkerOptionsValidator
    : IValidateOptions<ControlCloudOutboxWorkerOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        ControlCloudOutboxWorkerOptions options)
    {
        if (options.BatchSize is < 1 or > 100)
        {
            return ValidateOptionsResult.Fail(
                "ControlCloud:OutboxWorker:BatchSize must be between 1 and 100.");
        }

        if (options.PollIntervalSeconds is < 1 or > 300)
        {
            return ValidateOptionsResult.Fail(
                "ControlCloud:OutboxWorker:PollIntervalSeconds must be between 1 and 300.");
        }

        return ValidateOptionsResult.Success;
    }
}
