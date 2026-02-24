using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using MinecraftSkins.Api.Endpoints;

namespace MinecraftSkins.Tests.UnitTests.Api
{
    public class AuthEndpointsTests
    {
        [Fact]
        public void MapAuthEndpoints_RegistersLoginAndRegisterPostRoutes()
        {
            var endpoints = BuildEndpoints(app => app.MapAuthEndpoints());

            var login = endpoints.Single(e => e.RoutePattern.RawText == "/api/login");
            var register = endpoints.Single(e => e.RoutePattern.RawText == "/api/register");

            login.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Should().ContainSingle().Which.Should().Be("POST");
            register.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Should().ContainSingle().Which.Should().Be("POST");
        }

        private static List<RouteEndpoint> BuildEndpoints(Action<WebApplication> map)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddRouting();

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
