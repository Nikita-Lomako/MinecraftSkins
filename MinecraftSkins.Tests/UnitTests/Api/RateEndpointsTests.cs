using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using MinecraftSkins.Api.Endpoints;

namespace MinecraftSkins.Tests.UnitTests.Api
{
    public class RateEndpointsTests
    {
        [Fact]
        public void MapRateEndpoints_RegistersAdminOnlyBtcUsdRoute()
        {
            var endpoints = BuildEndpoints(app => app.MapRateEndpoints());
            var endpoint = endpoints.Single(e => e.RoutePattern.RawText == "/api/rates/btc-usd");

            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Should().ContainSingle().Which.Should().Be("GET");
            endpoint.Metadata.GetOrderedMetadata<AuthorizationPolicy>()
                .SelectMany(p => p.Requirements)
                .OfType<RolesAuthorizationRequirement>()
                .SelectMany(r => r.AllowedRoles)
                .Should()
                .Contain("Admin");
        }

        private static List<RouteEndpoint> BuildEndpoints(Action<WebApplication> map)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddRouting();
            builder.Services.AddAuthorization();

            var app = builder.Build();
            map(app);

            var routeBuilder = (IEndpointRouteBuilder)app;
            return routeBuilder.DataSources
                .SelectMany(ds => ds.Endpoints)
                .OfType<RouteEndpoint>()
                .ToList();
        }
    }
}
