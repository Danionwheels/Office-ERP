namespace SafarSuite.StagingPreflight;

internal static class StagingPreflightContract
{
    public static readonly string[] RequiredVariables =
    [
        "CONTROL_CLOUD_HOST",
        "CONTROL_DESK_HOST",
        "CONTROL_CLOUD_DB_NAME",
        "CONTROL_CLOUD_DB_USER",
        "CONTROL_CLOUD_DB_PASSWORD",
        "CONTROL_CLOUD_DB_HOST_PORT",
        "CONTROL_DESK_DB_NAME",
        "CONTROL_DESK_DB_USER",
        "CONTROL_DESK_DB_PASSWORD",
        "CONTROL_DESK_DB_HOST_PORT",
        "CONTROL_DESK_SESSION_SIGNING_SECRET",
        "CONTROL_DESK_OPERATOR_USER_ID",
        "CONTROL_DESK_OPERATOR_EMAIL",
        "CONTROL_DESK_OPERATOR_FULL_NAME",
        "CONTROL_DESK_OPERATOR_PASSWORD_HASH",
        "CONTROL_DESK_OPERATOR_ROLE",
        "CONTROL_DESK_OPERATOR_SCOPE",
        "CONTROL_DESK_PUBLISHER_SIGNING_KEY_ID",
        "CONTROL_DESK_PUBLISHER_SIGNING_SECRET",
        "CONTROL_CLOUD_ENTITLEMENT_SIGNING_KEY_ID",
        "CONTROL_CLOUD_APP_ACTIVATION_SIGNING_KEY_ID",
        "PROVIDER_ACCESS_SHARED_SECRET",
        "CLIENT_PORTAL_SESSION_SIGNING_SECRET",
        "CLIENT_PORTAL_MFA_PROTECTION_SECRET",
        "CLIENT_PORTAL_PUBLIC_URL",
        "CLIENT_PORTAL_INVITATION_DELIVERY_PROVIDER",
        "SMTP_HOST",
        "SMTP_PORT",
        "SMTP_USER",
        "SMTP_PASS",
        "FROM_ADDRESS"
    ];

    public static readonly string[] SecretFiles =
    [
        "control-desk-publisher-hmac",
        "control-cloud-entitlement-hmac",
        "app-activation-public.pem",
        "app-activation-private.pem",
        "provider-access-shared-secret",
        "provider-access-session-hmac",
        "provider-access-totp-hmac"
    ];

    public static readonly string[] SymmetricEnvironmentVariables =
    [
        "CONTROL_DESK_SESSION_SIGNING_SECRET",
        "CONTROL_DESK_PUBLISHER_SIGNING_SECRET",
        "PROVIDER_ACCESS_SHARED_SECRET",
        "CLIENT_PORTAL_SESSION_SIGNING_SECRET",
        "CLIENT_PORTAL_MFA_PROTECTION_SECRET"
    ];

    public static readonly string[] SymmetricSecretFiles =
    [
        "control-desk-publisher-hmac",
        "control-cloud-entitlement-hmac",
        "provider-access-shared-secret",
        "provider-access-session-hmac",
        "provider-access-totp-hmac"
    ];

    public static bool MayBeEmpty(string variable) =>
        variable is "SMTP_USER" or "SMTP_PASS";
}
