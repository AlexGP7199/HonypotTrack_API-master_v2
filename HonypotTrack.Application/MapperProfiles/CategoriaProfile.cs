using AutoMapper;
using HonypotTrack.Application.Dtos.Categoria;
using Entity = HoneypotTrack.Domain.Entities;

namespace HonypotTrack.Application.MapperProfiles;

public class CategoriaProfile : Profile
{
    public CategoriaProfile()
    {
        // Entity -> DTO
        CreateMap<Entity.Categoria, CategoriaDto>();

        // DTO -> Entity
        CreateMap<CategoriaCreateDto, Entity.Categoria>()
            .ForMember(dest => dest.CategoryId, opt => opt.Ignore())
            .ForMember(dest => dest.Transacciones, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaActualizacion, opt => opt.Ignore());

        CreateMap<CategoriaUpdateDto, Entity.Categoria>()
            .ForMember(dest => dest.Transacciones, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaActualizacion, opt => opt.Ignore());
    }
}
