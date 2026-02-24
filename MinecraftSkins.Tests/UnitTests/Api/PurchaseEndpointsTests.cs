using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using MinecraftSkins.Api.Endpoints;

namespace MinecraftSkins.Tests.UnitTests.Api;

public class PurchaseEndpointsTests
{
    [Fact]
    public void MapPurchaseEndpoints_RegistersAuthenticatedRoutes()
    {
        var endpoints = BuildEndpoints(app => app.MapPurchaseEndpoints());

        var create = endpoints.Single(e =>
            (e.RoutePattern.RawText is "/api/purchases/" or "/api/purchases") &&
            e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("POST") == true);
        var getAll = endpoints.Single(e =>
            (e.RoutePattern.RawText is "/api/purchases/" or "/api/purchases") &&
            e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("GET") == true);
        var getById = endpoints.Single(e => e.RoutePattern.RawText == "/api/purchases/{id}");

        create.Metadata.GetOrderedMetadata<IAuthorizeData>().Should().NotBeEmpty();
        getAll.Metadata.GetOrderedMetadata<IAuthorizeData>().Should().NotBeEmpty();
        getById.Metadata.GetOrderedMetadata<IAuthorizeData>().Should().NotBeEmpty();
    }

    private static List<RouteEndpoint> BuildEndpoints(Action<WebApplication> map)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddAuthorization();
        builder.Services.AddScoped(_ => new MinecraftSkins.Api.Filters.IdempotencyFilter());

        var app = builder.Build();
        map(app);

        var routeBuilder = (IEndpointRouteBuilder)app;
        return routeBuilder.DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();
    }
}

