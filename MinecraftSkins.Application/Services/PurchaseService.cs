using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
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

        // Проверка: пользователь может купить каждый скин только один раз
        var existingPurchases = await _purchaseRepository.GetAllAsync(buyerId, null, skinId, null, null, 0, 1, cancellationToken);
        if (existingPurchases.Any())
        {
            _logger.LogWarning("User {BuyerId} already purchased skin {SkinId}", buyerId, skinId);
            throw new InvalidOperationException("You have already purchased this skin");
        }

        // Получение курса BTC
        var btcRateResult = await _btcRateService.GetBtcUsdRateAsync(cancellationToken);
        
        // Расчет финальной цены
        var finalPrice = _priceCalculator.CalculateFinalPrice(skin.BasePriceUsd, btcRateResult.Rate);

        // Optimistic Concurrency: сохраняем RowVersion для проверки при сохранении
        var originalRowVersion = skin.RowVersion;
        
        var purchase = new Purchase
        {
            SkinId = skinId,
            BuyerId = buyerId,
            PriceUsdFinal = finalPrice,
            BtcUsdRate = btcRateResult.Rate,
            PurchasedAtUtc = DateTime.UtcNow
        };

        try
        {
            await _purchaseRepository.CreateAsync(purchase, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, 
                "Concurrency conflict detected when purchasing skin {SkinId} by buyer {BuyerId}. " +
                "Skin may have been modified or deleted by another user.", skinId, buyerId);
            
            // Перезагружаем скин из БД для проверки текущего состояния
            var currentSkin = await _skinRepository.GetByIdAsync(skinId, cancellationToken);
            
            if (currentSkin == null)
            {
                _logger.LogWarning("Skin {SkinId} was deleted during purchase attempt", skinId);
                throw new KeyNotFoundException($"Skin with id {skinId} was deleted and is no longer available");
            }
            
            if (currentSkin.IsDeleted)
            {
                _logger.LogWarning("Skin {SkinId} was soft-deleted during purchase attempt", skinId);
                throw new InvalidOperationException($"Skin with id {skinId} was deleted and is no longer available");
            }
            
            if (!currentSkin.IsAvailable)
            {
                _logger.LogWarning("Skin {SkinId} became unavailable during purchase attempt", skinId);
                throw new InvalidOperationException($"Skin with id {skinId} is no longer available for purchase");
            }
            
            // Если скин все еще доступен, но RowVersion изменился - это одновременная покупка
            // В этом случае выбрасываем исключение о конфликте конкурентности
            _logger.LogWarning("Concurrent purchase detected for skin {SkinId}. Another purchase may have occurred simultaneously.", skinId);
            throw new InvalidOperationException(
                "The skin was modified by another user. Please refresh and try again.");
        }

        _logger.LogInformation("Purchase created with id {PurchaseId} for skin {SkinId} by buyer {BuyerId}. Price: {Price}, Rate: {Rate}", 
            purchase.Id, skinId, buyerId, finalPrice, btcRateResult.Rate);

        var purchaseDto = _mapper.Map<PurchaseDto>(purchase);
        
        // Загружаем информацию о скине (уже есть в переменной skin, но для консистентности используем репозиторий)
        purchaseDto.Skin = _mapper.Map<SkinPurchaseDto>(skin);
        
        return purchaseDto;
    }

    public async Task<List<PurchaseDto>> GetPurchasesAsync(string? buyerId, string? buyerUserName, Guid? skinId, DateTime? from, DateTime? to, int skip, int take, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting purchases with buyerId={BuyerId}, buyerUserName={BuyerUserName}, skinId={SkinId}, from={From}, to={To}, skip={Skip}, take={Take}",
            buyerId, buyerUserName, skinId, from, to, skip, take);

        cancellationToken.ThrowIfCancellationRequested();

        var purchases = await _purchaseRepository.GetAllAsync(buyerId, buyerUserName, skinId, from, to, skip, take, cancellationToken);
        var purchaseDtos = _mapper.Map<List<PurchaseDto>>(purchases);

        // Загружаем информацию о скинах отдельными запросами (включая soft-deleted)
        // Оптимизация: загружаем параллельно для лучшей производительности
        var uniqueSkinIds = purchaseDtos.Select(p => p.SkinId).Distinct().ToList();
        var skinsDict = new Dictionary<Guid, SkinPurchaseDto>();
        
        if (uniqueSkinIds.Any())
        {
            var skinTasks = uniqueSkinIds.Select(async id =>
            {
                var skin = await _skinRepository.GetByIdIncludingDeletedAsync(id, cancellationToken);
                if (skin != null)
                {
                    return (id, _mapper.Map<SkinPurchaseDto>(skin));
                }
                return ((Guid id, SkinPurchaseDto dto)?)null;
            });
            
            var skinResults = await Task.WhenAll(skinTasks);
            
            foreach (var result in skinResults)
            {
                if (result.HasValue)
                {
                    skinsDict[result.Value.id] = result.Value.dto;
                }
            }
        }

        // Присваиваем информацию о скинах
        foreach (var purchaseDto in purchaseDtos)
        {
            if (skinsDict.TryGetValue(purchaseDto.SkinId, out var skinDto))
            {
                purchaseDto.Skin = skinDto;
            }

        }

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

        var purchaseDto = _mapper.Map<PurchaseDto>(purchase);
        
        // Загружаем информацию о скине отдельным запросом (включая soft-deleted)
        var skin = await _skinRepository.GetByIdIncludingDeletedAsync(purchase.SkinId, cancellationToken);
        if (skin != null)
        {
            purchaseDto.Skin = _mapper.Map<SkinPurchaseDto>(skin);
        }
        _logger.LogInformation("Found purchase with id {Id}", id);
        return purchaseDto;
    }
}
