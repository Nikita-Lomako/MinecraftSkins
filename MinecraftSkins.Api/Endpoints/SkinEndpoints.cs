using System.Threading;
using Microsoft.AspNetCore.Mvc;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Application.Services;

namespace MinecraftSkins.Api.Endpoints;

public static class SkinEndpoints
{
    public static void MapSkinEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/skins").WithTags("Skins");

        group.MapGet("/", GetAllSkins)
            .WithName("GetAllSkins")
            .Produces<List<SkinDto>>(200);

        group.MapGet("/{id}", GetSkinById)
            .WithName("GetSkinById")
            .Produces<SkinDto>(200)
            .Produces(404);

        group.MapPost("/", CreateSkin)
            .WithName("CreateSkin")
            .Accepts<SkinCreateDto>("application/json")
            .Produces<SkinDto>(201)
            .Produces(400);

        group.MapPut("/{id}", UpdateSkin)
            .WithName("UpdateSkin")
            .Accepts<SkinUpdateDto>("application/json")
            .Produces<SkinDto>(200)
            .Produces(400)
            .Produces(404);

        group.MapDelete("/{id}", DeleteSkin)
            .WithName("DeleteSkin")
            .Produces(204)
            .Produces(404);
    }

    private static async Task<IResult> GetAllSkins(
        [FromServices] ISkinService skinService,
        [FromQuery] bool? availableOnly,
        [FromQuery] string? search,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var skins = await skinService.GetAllSkinsAsync(availableOnly, search, skip, take, cancellationToken);
            return Results.Ok(skins);
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(499); // Client Closed Request
        }
        catch (Exception)
        {
            return Results.Problem("An error occurred while retrieving skins", statusCode: 500);
        }
    }

    private static async Task<IResult> GetSkinById(
        Guid id,
        [FromServices] ISkinService skinService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var skin = await skinService.GetSkinByIdAsync(id, cancellationToken);
            if (skin == null)
                return Results.NotFound();
            return Results.Ok(skin);
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(499);
        }
        catch (Exception)
        {
            return Results.Problem("An error occurred while retrieving the skin", statusCode: 500);
        }
    }

    private static async Task<IResult> CreateSkin(
        [FromServices] ISkinService skinService,
        [FromBody] SkinCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var createdSkin = await skinService.CreateSkinAsync(dto, cancellationToken);
            return Results.Created($"/api/skins/{createdSkin.Id}", createdSkin);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(499);
        }
        catch (Exception)
        {
            return Results.Problem("An error occurred while creating the skin", statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateSkin(
        Guid id,
        [FromServices] ISkinService skinService,
        [FromBody] SkinUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updatedSkin = await skinService.UpdateSkinAsync(id, dto, cancellationToken);
            if (updatedSkin == null)
                return Results.NotFound();
            return Results.Ok(updatedSkin);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(499);
        }
        catch (Exception)
        {
            return Results.Problem("An error occurred while updating the skin", statusCode: 500);
        }
    }

    private static async Task<IResult> DeleteSkin(
        Guid id,
        [FromServices] ISkinService skinService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await skinService.DeleteSkinAsync(id, cancellationToken);
            if (!deleted)
                return Results.NotFound();
            return Results.NoContent();
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(499);
        }
        catch (Exception)
        {
            return Results.Problem("An error occurred while deleting the skin", statusCode: 500);
        }
    }
}

