using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SafarSuite.ControlDesk.Api.Modules.Auth;

public static class ControlDeskAuthServiceCollectionExtensions
{
    public static IServiceCollection AddControlDeskAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ControlDeskOperatorAccessOptions>()
            .Bind(configuration.GetSection(ControlDeskOperatorAccessOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ControlDeskOperatorAccessOptions>, ControlDeskOperatorAccessOptionsValidator>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IControlDeskSessionTokenService, ControlDeskSessionTokenService>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = ControlDeskSessionTokenService.AuthenticationScheme;
                options.DefaultChallengeScheme = ControlDeskSessionTokenService.AuthenticationScheme;
                options.DefaultForbidScheme = ControlDeskSessionTokenService.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, ControlDeskBearerAuthenticationHandler>(
                ControlDeskSessionTokenService.AuthenticationScheme,
                _ => { });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            AddScopePolicy(options, ControlDeskPolicies.CommandCenterRead, ControlDeskScopes.CommandCenterRead);
            AddScopePolicy(options, ControlDeskPolicies.ClientsManage, ControlDeskScopes.ClientsManage);
            AddScopePolicy(options, ControlDeskPolicies.ContractsManage, ControlDeskScopes.ContractsManage);
            AddScopePolicy(options, ControlDeskPolicies.AccountingManage, ControlDeskScopes.AccountingManage);
            AddScopePolicy(options, ControlDeskPolicies.BillingManage, ControlDeskScopes.BillingManage);
            AddScopePolicy(options, ControlDeskPolicies.PaymentsManage, ControlDeskScopes.PaymentsManage);
            AddScopePolicy(options, ControlDeskPolicies.EntitlementsManage, ControlDeskScopes.EntitlementsManage);
            AddScopePolicy(options, ControlDeskPolicies.ControlCloudManage, ControlDeskScopes.ControlCloudManage);
            AddScopePolicy(options, ControlDeskPolicies.ReportsRead, ControlDeskScopes.ReportsRead);
        });

        return services;
    }

    private static void AddScopePolicy(
        AuthorizationOptions options,
        string policyName,
        string requiredScope)
    {
        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                context.User.HasClaim(ControlDeskSessionTokenService.ScopeClaimType, ControlDeskScopes.Admin)
                || context.User.HasClaim(ControlDeskSessionTokenService.ScopeClaimType, requiredScope));
        });
    }
}
