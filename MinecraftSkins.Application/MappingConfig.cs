using System;
using AutoMapper;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Application;

public class MappingConfig : Profile
{
    public MappingConfig()
    {
        // Skin mappings
        CreateMap<Skin, SkinDto>()
            .ForMember(dest => dest.FinalPrice, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentBtcRate, opt => opt.Ignore());
        CreateMap<Skin, SkinPurchaseDto>();
        CreateMap<SkinCreateDto, Skin>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());
        
        CreateMap<SkinUpdateDto, Skin>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAtUtc, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        // Purchase mappings
        CreateMap<Purchase, PurchaseDto>();
        
        CreateMap<PurchaseCreateDto, Purchase>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PriceUsdFinal, opt => opt.Ignore())
            .ForMember(dest => dest.BtcUsdRate, opt => opt.Ignore())
            .ForMember(dest => dest.PurchasedAtUtc, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.BuyerId, opt => opt.Ignore())
            .ForMember(dest => dest.Skin, opt => opt.Ignore());
    }
}
