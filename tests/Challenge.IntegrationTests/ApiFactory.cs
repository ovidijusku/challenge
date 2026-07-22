using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;

namespace Challenge.IntegrationTests;

/// <summary>
/// Boots the real API against a disposable SQL container. Migrations are applied
/// automatically by the application on startup.
/// </summary>
/// <remarks>
/// Uses Azure SQL Edge, which runs natively on arm64 (the standard mssql/server image
/// only ships for amd64 and crashes under QEMU emulation on Apple Silicon). Because that
/// image does not bundle the <c>sqlcmd</c> tool used by the default readiness probe, the
/// wait strategy keys off the server's "ready for client connections" log line instead.
/// </remarks>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _database =
        new MsSqlBuilder("mcr.microsoft.com/azure-sql-edge:latest")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("SQL Server is now ready for client connections"))
            .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _database.GetConnectionString());
    }

    public async Task InitializeAsync() => await _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }
}
