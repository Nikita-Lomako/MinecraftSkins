using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Testcontainers.PostgreSql;
using MinecraftSkins.Api.Handlers;
using MinecraftSkins.Infrastructure.Data;
using MinecraftSkins.Domain.Interfaces;
using MinecraftSkins.Tests.Mocks;

namespace MinecraftSkins.Tests.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<GlobalExceptionHandler>, IAsyncLifetime
{
    private const string TestJwtSecret = "test-secret-key-for-jwt-token-generation-in-tests-min-32-chars";

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:15-alpine")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ensure test secret is available regardless of configuration source precedence.
        builder.UseSetting("ApiSettings:Secret", TestJwtSecret);
        builder.UseSetting("ApiSettings__Secret", TestJwtSecret);

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override configuration for tests
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ApiSettings:Secret", TestJwtSecret },
                { "PriceCalculator:ReferenceBtcRate", "68000" },
                { "PriceCalculator:LiquidityFee", "0.02" },
                { "BtcRateProvider:Provider", "Fake" }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove real DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(AppDbContext));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Add test DbContext with Testcontainers PostgreSQL
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_postgresContainer.GetConnectionString());
            });

            // Replace Redis with in-memory cache for tests
            var redisDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDistributedCache));
            if (redisDescriptor != null)
            {
                services.Remove(redisDescriptor);
            }
            services.AddDistributedMemoryCache();

            // Replace IBtcRateProvider with fake
            var btcProviderDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IBtcRateProvider));
            if (btcProviderDescriptor != null)
            {
                services.Remove(btcProviderDescriptor);
            }
            services.AddSingleton<IBtcRateProvider>(_ => new FakeBtcRateProvider(68000m));
        });
    }

    public async ValueTask InitializeAsync()
    {
        await _postgresContainer.StartAsync();
    }

    public new async ValueTask DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
