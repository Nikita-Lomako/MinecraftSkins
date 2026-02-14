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
        CreateMap<Skin, SkinDto>();
        CreateMap<SkinCreateDto, Skin>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAtUtc, opt => opt.Ignore());
        
        CreateMap<SkinUpdateDto, Skin>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAtUtc, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAtUtc, opt => opt.Ignore());

        // Purchase mappings
        CreateMap<Purchase, PurchaseDto>()
            .ForMember(dest => dest.SkinName, opt => opt.MapFrom(src => src.Skin != null ? src.Skin.Name : null));
        
        CreateMap<PurchaseCreateDto, Purchase>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PriceUsdFinal, opt => opt.Ignore())
            .ForMember(dest => dest.BtcUsdRate, opt => opt.Ignore())
            .ForMember(dest => dest.PurchasedAtUtc, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.BuyerId, opt => opt.Ignore())
            .ForMember(dest => dest.Skin, opt => opt.Ignore());
    }
}
