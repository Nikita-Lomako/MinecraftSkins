using System;
using System.Collections.Generic;
using System.Threading;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Domain.IRepositories;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Application.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly ISkinRepository _skinRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<PurchaseCreateDto> _createValidator;
    private readonly ILogger<PurchaseService> _logger;

    public PurchaseService(
        IPurchaseRepository purchaseRepository,
        ISkinRepository skinRepository,
        IMapper mapper,
        IValidator<PurchaseCreateDto> createValidator,
        ILogger<PurchaseService> logger)
    {
        _purchaseRepository = purchaseRepository;
        _skinRepository = skinRepository;
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

        // TODO: Блок 3 - Получение курса через IBtcRateService
        // TODO: Блок 3 - Расчет цены через IPriceCalculator
        // TODO: Блок 3 - Конкурентность: Optimistic Concurrency (ConcurrencyToken в Skin)
        // Пока используем заглушки
        var btcRate = 68000m; // Заглушка
        var finalPrice = skin.BasePriceUsd; // Заглушка

        var purchase = new Purchase
        {
            SkinId = skinId,
            BuyerId = buyerId,
            PriceUsdFinal = finalPrice,
            BtcUsdRate = btcRate,
            PurchasedAtUtc = DateTime.UtcNow
        };

        await _purchaseRepository.CreateAsync(purchase, cancellationToken);

        _logger.LogInformation("Purchase created with id {PurchaseId} for skin {SkinId} by buyer {BuyerId}", 
            purchase.Id, skinId, buyerId);

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
