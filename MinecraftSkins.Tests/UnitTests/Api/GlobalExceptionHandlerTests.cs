using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinecraftSkins.Api.Handlers;

namespace MinecraftSkins.Tests.UnitTests.Api;

public class GlobalExceptionHandlerTests
{
    private readonly ILogger<GlobalExceptionHandler> _logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
    private readonly IProblemDetailsService _problemDetailsService = Substitute.For<IProblemDetailsService>();

    [Fact]
    public async Task TryHandleAsync_InvalidOperationUnavailable_MapsTo503()
    {
        var ct = TestContext.Current.CancellationToken;
        var httpContext = CreateHttpContext();
        ProblemDetailsContext? captured = null;
        _problemDetailsService.TryWriteAsync(Arg.Do<ProblemDetailsContext>(x => captured = x))
            .Returns(ValueTask.FromResult(true));

        var sut = new GlobalExceptionHandler(_logger, _problemDetailsService);

        var handled = await sut.TryHandleAsync(httpContext, new InvalidOperationException("BTC rate unavailable"), ct);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        captured.Should().NotBeNull();
        captured!.ProblemDetails.Title.Should().Be("Service Unavailable");
        captured.ProblemDetails.Detail.Should().Be("BTC rate unavailable");
    }

    [Fact]
    public async Task TryHandleAsync_InvalidOperationOther_MapsTo409()
    {
        var ct = TestContext.Current.CancellationToken;
        var httpContext = CreateHttpContext();
        ProblemDetailsContext? captured = null;
        _problemDetailsService.TryWriteAsync(Arg.Do<ProblemDetailsContext>(x => captured = x))
            .Returns(ValueTask.FromResult(true));

        var sut = new GlobalExceptionHandler(_logger, _problemDetailsService);

        var handled = await sut.TryHandleAsync(httpContext, new InvalidOperationException("already purchased"), ct);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        captured.Should().NotBeNull();
        captured!.ProblemDetails.Title.Should().Be("Conflict");
    }

    [Fact]
    public async Task TryHandleAsync_ValidationException_AddsErrorsExtension()
    {
        var ct = TestContext.Current.CancellationToken;
        var httpContext = CreateHttpContext();
        ProblemDetailsContext? captured = null;
        _problemDetailsService.TryWriteAsync(Arg.Do<ProblemDetailsContext>(x => captured = x))
            .Returns(ValueTask.FromResult(true));

        var validationException = new FluentValidation.ValidationException(new List<ValidationFailure>
        {
            new("SkinId", "SkinId is required"),
            new("SkinId", "SkinId must not be empty")
        });

        var sut = new GlobalExceptionHandler(_logger, _problemDetailsService);

        var handled = await sut.TryHandleAsync(httpContext, validationException, ct);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        captured.Should().NotBeNull();
        captured!.ProblemDetails.Title.Should().Be("Validation Error");
        captured.ProblemDetails.Extensions.Should().ContainKey("errors");
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/purchases";
        return context;
    }
}

