using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using MinecraftSkins.Api.Endpoints;

namespace MinecraftSkins.Tests.UnitTests.Api
{
    public class SkinEndpointsTests
    {
        [Fact]
        public void MapSkinEndpoints_RegistersExpectedRoutes()
        {
            var endpoints = BuildEndpoints(app => app.MapSkinEndpoints());

            endpoints.Should().Contain(e => e.RoutePattern.RawText == "/api/skins/" || e.RoutePattern.RawText == "/api/skins");
            endpoints.Should().Contain(e => e.RoutePattern.RawText == "/api/skins/{id}");
            endpoints.Count(e => e.RoutePattern.RawText == "/api/skins/{id}").Should().Be(3);
        }

        [Fact]
        public void MapSkinEndpoints_AdminRoutesRequireAdminRole()
        {
            var endpoints = BuildEndpoints(app => app.MapSkinEndpoints());

            var create = endpoints.Single(e =>
                (e.RoutePattern.RawText == "/api/skins/" || e.RoutePattern.RawText == "/api/skins") &&
                e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("POST") == true);
            var update = endpoints.Single(e =>
                e.RoutePattern.RawText == "/api/skins/{id}" &&
                e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("PUT") == true);
            var delete = endpoints.Single(e =>
                e.RoutePattern.RawText == "/api/skins/{id}" &&
                e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("DELETE") == true);

            create.Metadata.GetOrderedMetadata<AuthorizationPolicy>()
                .SelectMany(p => p.Requirements)
                .OfType<RolesAuthorizationRequirement>()
                .SelectMany(r => r.AllowedRoles)
                .Should().Contain("Admin");
            update.Metadata.GetOrderedMetadata<AuthorizationPolicy>()
                .SelectMany(p => p.Requirements)
                .OfType<RolesAuthorizationRequirement>()
                .SelectMany(r => r.AllowedRoles)
                .Should().Contain("Admin");
            delete.Metadata.GetOrderedMetadata<AuthorizationPolicy>()
                .SelectMany(p => p.Requirements)
                .OfType<RolesAuthorizationRequirement>()
                .SelectMany(r => r.AllowedRoles)
                .Should().Contain("Admin");
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
