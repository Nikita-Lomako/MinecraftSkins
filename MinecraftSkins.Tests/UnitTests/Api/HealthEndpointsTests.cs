using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MinecraftSkins.Api.Endpoints;

namespace MinecraftSkins.Tests.UnitTests.Api;

public class HealthEndpointsTests
{
    [Fact]
    public void MapHealthEndpoints_RegistersAnonymousHealthRoute()
    {
        var endpoints = BuildEndpoints(app => app.MapHealthEndpoints());
        var endpoint = endpoints.Single(e => e.RoutePattern.RawText == "/health");

        endpoint.Metadata.GetMetadata<IAllowAnonymous>().Should().NotBeNull();
    }

    private static List<RouteEndpoint> BuildEndpoints(Action<WebApplication> map)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddHealthChecks();

        var app = builder.Build();
        map(app);

        var routeBuilder = (IEndpointRouteBuilder)app;
        return routeBuilder.DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();
    }
}

