using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using MinecraftSkins.Api.Filters;

namespace MinecraftSkins.Tests.UnitTests.Api;

public class IdempotencyFilterTests
{
    [Fact]
    public async Task InvokeAsync_WithoutHeader_ReturnsBadRequest_AndSkipsNext()
    {
        var ct = TestContext.Current.CancellationToken;
        var cache = Substitute.For<IDistributedCache>();
        var context = CreateContext(cache);
        var sut = new IdempotencyFilter();
        var nextCalled = false;

        var result = await sut.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            });

        nextCalled.Should().BeFalse();
        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        ((IStatusCodeHttpResult)result!).StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        await cache.DidNotReceiveWithAnyArgs().GetAsync(default!, ct);
    }

    [Fact]
    public async Task InvokeAsync_WithCachedResponse_ReturnsCachedResult_AndSkipsNext()
    {
        var cache = Substitute.For<IDistributedCache>();
        var key = Guid.NewGuid();
        var context = CreateContext(cache, key);
        var cached = JsonSerializer.Serialize(new { StatusCode = 201, Value = new { id = "cached-id" } });
        cache.GetAsync($"Idempotent_{key}", Arg.Any<CancellationToken>())
            .Returns(Encoding.UTF8.GetBytes(cached));
        var sut = new IdempotencyFilter();
        var nextCalled = false;

        var result = await sut.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            });

        nextCalled.Should().BeFalse();
        result.Should().BeAssignableTo<IResult>();

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        await ((IResult)result!).ExecuteAsync(httpContext);
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task InvokeAsync_WithSuccessfulResponse_CachesResult()
    {
        var cache = Substitute.For<IDistributedCache>();
        var key = Guid.NewGuid();
        var context = CreateContext(cache, key);
        cache.GetAsync($"Idempotent_{key}", Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        var sut = new IdempotencyFilter(5);

        var result = await sut.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.Created("/api/purchases/1", new { id = "1" })));

        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        ((IStatusCodeHttpResult)result!).StatusCode.Should().Be(StatusCodes.Status201Created);

        await cache.Received(1).SetAsync(
            $"Idempotent_{key}",
            Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(o =>
                o.AbsoluteExpirationRelativeToNow.HasValue &&
                o.AbsoluteExpirationRelativeToNow.Value.TotalMinutes == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_WithErrorResponse_DoesNotCache()
    {
        var ct = TestContext.Current.CancellationToken;
        var cache = Substitute.For<IDistributedCache>();
        var key = Guid.NewGuid();
        var context = CreateContext(cache, key);
        cache.GetAsync($"Idempotent_{key}", Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        var sut = new IdempotencyFilter();

        var result = await sut.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.BadRequest(new { error = "invalid request" })));

        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        ((IStatusCodeHttpResult)result!).StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        await cache.DidNotReceiveWithAnyArgs().SetAsync(default!, default!, default!, ct);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-00000000000Z")]
    public async Task InvokeAsync_WithBrokenHeaderVariations_ReturnsBadRequest(string brokenHeader)
    {
        var ct = TestContext.Current.CancellationToken;
        var cache = Substitute.For<IDistributedCache>();
        var context = CreateContextWithRawHeader(cache, brokenHeader);
        var sut = new IdempotencyFilter();
        var nextCalled = false;

        var result = await sut.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok(new { id = "unexpected" }));
            });

        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        ((IStatusCodeHttpResult)result!).StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        nextCalled.Should().BeFalse();
        await cache.DidNotReceiveWithAnyArgs().SetAsync(default!, default!, default!, ct);
    }

    [Fact]
    public async Task InvokeAsync_WithNoContentResult_DoesNotCache()
    {
        var ct = TestContext.Current.CancellationToken;
        var cache = Substitute.For<IDistributedCache>();
        var key = Guid.NewGuid();
        var context = CreateContext(cache, key);
        cache.GetAsync($"Idempotent_{key}", Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        var sut = new IdempotencyFilter();

        var result = await sut.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(Results.NoContent()));

        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        ((IStatusCodeHttpResult)result!).StatusCode.Should().Be(StatusCodes.Status204NoContent);
        await cache.DidNotReceiveWithAnyArgs().SetAsync(default!, default!, default!, ct);
    }

    private static EndpointFilterInvocationContext CreateContext(IDistributedCache cache, Guid? key = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(cache);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        if (key.HasValue)
        {
            httpContext.Request.Headers["Idempotency-Key"] = key.Value.ToString();
        }

        return new TestEndpointFilterInvocationContext(httpContext);
    }

    private static EndpointFilterInvocationContext CreateContextWithRawHeader(IDistributedCache cache, string rawHeader)
    {
        var services = new ServiceCollection();
        services.AddSingleton(cache);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        httpContext.Request.Headers["Idempotency-Key"] = rawHeader;
        return new TestEndpointFilterInvocationContext(httpContext);
    }

    private sealed class TestEndpointFilterInvocationContext(HttpContext httpContext) : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;
        public override IList<object?> Arguments { get; } = [];

        public override T GetArgument<T>(int index)
        {
            return (T)Arguments[index]!;
        }
    }
}

