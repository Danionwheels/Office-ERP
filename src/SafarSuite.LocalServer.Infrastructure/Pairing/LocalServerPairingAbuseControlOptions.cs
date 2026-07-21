namespace SafarSuite.LocalServer.Infrastructure.Pairing;

public sealed class LocalServerPairingAbuseControlOptions
{
    public const string SectionName = "LocalServer:PairingAbuseControls";

    public bool Enabled { get; set; } = true;

    public int AnonymousRequestBodyLimitBytes { get; set; } = 16 * 1024;

    public int AnonymousConcurrentRequestsPerRemote { get; set; } = 4;

    public int DiscoveryRequestsPerRemotePerMinute { get; set; } = 120;

    public int HelloRequestsPerRemotePerMinute { get; set; } = 60;

    public int PairingRequestsPerRemotePerHour { get; set; } = 12;

    public int PairingRequestsPerDeviceInstallIdPerDay { get; set; } = 4;

    public int PairingRequestsPerFingerprintPerDay { get; set; } = 4;

    public int PairingStatusPollsPerRequestPerMinute { get; set; } = 30;

    public int CredentialFailuresPerDevicePer15Minutes { get; set; } = 10;

    public int LoginFailuresPerActorPer15Minutes { get; set; } = 10;

    public int PendingQueueMaxRecords { get; set; } = 100;

    public int PendingRequestTtlHours { get; set; } = 48;

    public int DuplicateCoalescingWindowHours { get; set; } = 24;

    public int TemporaryQuarantineMinutes { get; set; } = 30;

    public int ManualDenyTtlHours { get; set; } = 24;

    public int SecurityEventReadLimit { get; set; } = 100;

    public int SecurityEventRetentionDays { get; set; } = 30;

    public int SecurityEventMaxRecords { get; set; } = 5000;

    public string SecurityEventStorePath { get; set; } =
        "App_Data/local-server-pairing-security-events.json";
}
