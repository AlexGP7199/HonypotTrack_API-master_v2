using AutoMapper;
using HonypotTrack.Application.Dtos.Cuenta;
using Entity = HoneypotTrack.Domain.Entities;

namespace HonypotTrack.Application.MapperProfiles;

public class CuentaProfile : Profile
{
    public CuentaProfile()
    {
        // Entity -> DTO
        CreateMap<Entity.Cuenta, CuentaDto>()
            .ForMember(dest => dest.UsuarioNombre, opt => opt.MapFrom(src => src.Usuario != null ? src.Usuario.FullName : null));

        // DTO -> Entity
        CreateMap<CuentaCreateDto, Entity.Cuenta>()
            .ForMember(dest => dest.AccountId, opt => opt.Ignore())
            .ForMember(dest => dest.Usuario, opt => opt.Ignore())
            .ForMember(dest => dest.Transacciones, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaActualizacion, opt => opt.Ignore());

        CreateMap<CuentaUpdateDto, Entity.Cuenta>()
            .ForMember(dest => dest.Usuario, opt => opt.Ignore())
            .ForMember(dest => dest.Transacciones, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaActualizacion, opt => opt.Ignore());
    }
}
