namespace SafarSuite.ControlDesk.Application.Modules.Diagnostics.Ports;

public static class OfficeDatabaseReadinessCodes
{
    public const string Ready = "OfficeDatabaseReady";

    public const string EphemeralPersistenceReady = "OfficeEphemeralPersistenceReady";

    public const string DatabaseUnavailable = "OfficeDatabaseUnavailable";

    public const string MigrationsPending = "OfficeDatabaseMigrationsPending";

    public const string UnknownMigrationsApplied = "OfficeDatabaseUnknownMigrationsApplied";

    public const string MigrationOrderMismatch = "OfficeDatabaseMigrationOrderMismatch";

    public const string InspectionTimedOut = "OfficeDatabaseInspectionTimedOut";

    public const string InspectionFailed = "OfficeDatabaseInspectionFailed";
}
