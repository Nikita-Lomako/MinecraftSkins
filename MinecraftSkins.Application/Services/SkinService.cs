using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Application.Interfaces;
using MinecraftSkins.Domain.IRepositories;
using MinecraftSkins.Domain.Models; // Ensure this is present

namespace MinecraftSkins.Application.Services;

public class SkinService : ISkinService
{
    private readonly ISkinRepository _skinRepository;
    private readonly IBtcRateService _btcRateService;
    private readonly IPriceCalculator _priceCalculator;
    private readonly IMapper _mapper;
    private readonly IValidator<SkinCreateDto> _createValidator;
    private readonly IValidator<SkinUpdateDto> _updateValidator;
    private readonly ILogger<SkinService> _logger;

    public SkinService(
        ISkinRepository skinRepository,
        IBtcRateService btcRateService,
        IPriceCalculator priceCalculator,
        IMapper mapper,
        IValidator<SkinCreateDto> createValidator,
        IValidator<SkinUpdateDto> updateValidator,
        ILogger<SkinService> logger)
    {
        _skinRepository = skinRepository;
        _btcRateService = btcRateService;
        _priceCalculator = priceCalculator;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<List<SkinDto>> GetAllSkinsAsync(bool? availableOnly, string? search, int skip, int take, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all skins with availableOnly={AvailableOnly}, search={Search}, skip={Skip}, take={Take}",
            availableOnly, search, skip, take);

        cancellationToken.ThrowIfCancellationRequested();

        var skins = await _skinRepository.GetAllAsync(availableOnly, search, skip, take, cancellationToken);
        var skinDtos = _mapper.Map<List<SkinDto>>(skins);

        // Получение курса BTC и расчет цен
        // Используем try-catch, чтобы вернуть список скинов даже если курс недоступен.
        // Если курс недоступен, FinalPrice и CurrentBtcRate будут null.
        
        BtcRateResult? btcRateResult = null;
        try
        {
            btcRateResult = await _btcRateService.GetBtcUsdRateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve BTC rate. Returning skins without price calculation.");
        }
        
        foreach (var dto in skinDtos)
        {
            if (btcRateResult is not null)
        {
            dto.FinalPrice = _priceCalculator.CalculateFinalPrice(dto.BasePriceUsd, btcRateResult.Rate);
            dto.CurrentBtcRate = btcRateResult.Rate;
            }
        }

        _logger.LogInformation("Retrieved {Count} skins", skinDtos.Count);
        return skinDtos;
    }

    public async Task<SkinDto?> GetSkinByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting skin with id {Id}", id);

        cancellationToken.ThrowIfCancellationRequested();

        var skin = await _skinRepository.GetByIdAsync(id, cancellationToken);
        if (skin == null)
        {
            _logger.LogWarning("Skin with id {Id} not found", id);
            return null;
        }

        var skinDto = _mapper.Map<SkinDto>(skin);

        // Получение курса BTC и расчет цены
        var btcRateResult = await _btcRateService.GetBtcUsdRateAsync(cancellationToken);
        
        skinDto.FinalPrice = _priceCalculator.CalculateFinalPrice(skinDto.BasePriceUsd, btcRateResult.Rate);
        skinDto.CurrentBtcRate = btcRateResult.Rate;

        _logger.LogInformation("Found skin with id {Id}", id);
        return skinDto;
    }

    public async Task<SkinDto> CreateSkinAsync(SkinCreateDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating skin with name {Name}", dto.Name);

        cancellationToken.ThrowIfCancellationRequested();

        var validationResult = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Validation failed for skin creation: {Errors}", errors);
            throw new ArgumentException($"Validation failed: {errors}");
        }

        var skin = _mapper.Map<Domain.Models.Skin>(dto);
        skin.CreatedAtUtc = DateTime.UtcNow;

        await _skinRepository.CreateAsync(skin, cancellationToken);

        _logger.LogInformation("Created skin with id {Id}", skin.Id);
        return _mapper.Map<SkinDto>(skin);
    }

    public async Task<SkinDto?> UpdateSkinAsync(Guid id, SkinUpdateDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating skin with id {Id}", id);

        cancellationToken.ThrowIfCancellationRequested();

        var validationResult = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Validation failed for skin update: {Errors}", errors);
            throw new ArgumentException($"Validation failed: {errors}");
        }

        var existingSkin = await _skinRepository.GetByIdAsync(id, cancellationToken);
        if (existingSkin == null)
        {
            _logger.LogWarning("Skin with id {Id} not found", id);
            return null;
        }

        _mapper.Map(dto, existingSkin);
        existingSkin.UpdatedAtUtc = DateTime.UtcNow;

        await _skinRepository.UpdateAsync(existingSkin, cancellationToken);

        _logger.LogInformation("Updated skin with id {Id}", id);
        return _mapper.Map<SkinDto>(existingSkin);
    }

    public async Task<bool> DeleteSkinAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting skin with id {Id}", id);

        cancellationToken.ThrowIfCancellationRequested();

        var existingSkin = await _skinRepository.GetByIdAsync(id, cancellationToken);
        if (existingSkin == null)
        {
            _logger.LogWarning("Skin with id {Id} not found", id);
            return false;
        }

        await _skinRepository.DeleteAsync(id, cancellationToken);

        _logger.LogInformation("Deleted skin with id {Id}", id);
        return true;
    }
}
