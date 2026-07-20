using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SafarSuite.ControlDesk.Application.Modules.Auth.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Auth;
using SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;
using SafarSuite.ControlDesk.Infrastructure.Security;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class PersistedOperatorControlDeskApiFactory : ControlDeskApiFactory
{
    public const string PersistedEmail = "persisted.operator@example.test";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            var localOperator = LocalOperator.CreateFirstAdministrator(
                LocalOperatorId.Create(Guid.Parse("8d670960-97da-4de6-9190-2d6aa810caa2")),
                LocalOperatorEmail.Create(PersistedEmail),
                "Persisted Test Operator",
                new Pbkdf2LocalOperatorPasswordCodec().Hash(Password),
                new DateTimeOffset(2026, 7, 20, 0, 0, 0, TimeSpan.Zero));
            var repository = new InMemoryLocalOperatorRepository([localOperator]);

            services.RemoveAll<ILocalOperatorRepository>();
            services.AddSingleton<ILocalOperatorRepository>(repository);
        });
    }
}
