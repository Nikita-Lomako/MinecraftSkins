using System;
using System.Collections.Generic;
using System.Threading;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Application.Interfaces;
using MinecraftSkins.Domain.IRepositories;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Application.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly ISkinRepository _skinRepository;
    private readonly IBtcRateService _btcRateService;
    private readonly IPriceCalculator _priceCalculator;
    private readonly IMapper _mapper;
    private readonly IValidator<PurchaseCreateDto> _createValidator;
    private readonly ILogger<PurchaseService> _logger;

    public PurchaseService(
        IPurchaseRepository purchaseRepository,
        ISkinRepository skinRepository,
        IBtcRateService btcRateService,
        IPriceCalculator priceCalculator,
        IMapper mapper,
        IValidator<PurchaseCreateDto> createValidator,
        ILogger<PurchaseService> logger)
    {
        _purchaseRepository = purchaseRepository;
        _skinRepository = skinRepository;
        _btcRateService = btcRateService;
        _priceCalculator = priceCalculator;
        _mapper = mapper;
        _createValidator = createValidator;
        _logger = logger;
    }

    public async Task<PurchaseDto> PurchaseSkinAsync(Guid skinId, string buyerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Purchase attempt for skin {SkinId} by buyer {BuyerId}", skinId, buyerId);

        cancellationToken.ThrowIfCancellationRequested();

        // Валидация: скин существует, не удален, доступен
        var skin = await _skinRepository.GetByIdAsync(skinId, cancellationToken);
        if (skin == null)
        {
            _logger.LogWarning("Skin with id {SkinId} not found", skinId);
            throw new KeyNotFoundException($"Skin with id {skinId} not found");
        }

        if (!skin.IsAvailable)
        {
            _logger.LogWarning("Skin with id {SkinId} is not available for purchase", skinId);
            throw new InvalidOperationException($"Skin with id {skinId} is not available for purchase");
        }

        // Получение курса BTC
        var btcRateResult = await _btcRateService.GetBtcUsdRateAsync(cancellationToken);
        
        // Расчет финальной цены
        var finalPrice = _priceCalculator.CalculateFinalPrice(skin.BasePriceUsd, btcRateResult.Rate);

        // TODO: Optimistic Concurrency (ConcurrencyToken в Skin) будет добавлен позже
        
        var purchase = new Purchase
        {
            SkinId = skinId,
            BuyerId = buyerId,
            PriceUsdFinal = finalPrice,
            BtcUsdRate = btcRateResult.Rate,
            PurchasedAtUtc = DateTime.UtcNow
        };

        await _purchaseRepository.CreateAsync(purchase, cancellationToken);

        _logger.LogInformation("Purchase created with id {PurchaseId} for skin {SkinId} by buyer {BuyerId}. Price: {Price}, Rate: {Rate}", 
            purchase.Id, skinId, buyerId, finalPrice, btcRateResult.Rate);

        return _mapper.Map<PurchaseDto>(purchase);
    }

    public async Task<List<PurchaseDto>> GetPurchasesAsync(string? buyerId, Guid? skinId, DateTime? from, DateTime? to, int skip, int take, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting purchases with buyerId={BuyerId}, skinId={SkinId}, from={From}, to={To}, skip={Skip}, take={Take}",
            buyerId, skinId, from, to, skip, take);

        cancellationToken.ThrowIfCancellationRequested();

        var purchases = await _purchaseRepository.GetAllAsync(buyerId, skinId, from, to, skip, take, cancellationToken);
        var purchaseDtos = _mapper.Map<List<PurchaseDto>>(purchases);

        _logger.LogInformation("Retrieved {Count} purchases", purchaseDtos.Count);
        return purchaseDtos;
    }

    public async Task<PurchaseDto?> GetPurchaseByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting purchase with id {Id}", id);

        cancellationToken.ThrowIfCancellationRequested();

        var purchase = await _purchaseRepository.GetByIdAsync(id, cancellationToken);
        if (purchase == null)
        {
            _logger.LogWarning("Purchase with id {Id} not found", id);
            return null;
        }

        _logger.LogInformation("Found purchase with id {Id}", id);
        return _mapper.Map<PurchaseDto>(purchase);
    }
}
