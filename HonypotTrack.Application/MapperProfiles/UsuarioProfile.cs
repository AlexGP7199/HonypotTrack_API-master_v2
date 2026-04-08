using AutoMapper;
using HonypotTrack.Application.Dtos.Usuario;
using Entity = HoneypotTrack.Domain.Entities;

namespace HonypotTrack.Application.MapperProfiles;

public class UsuarioProfile : Profile
{
    public UsuarioProfile()
    {
        // Entity -> DTO
        CreateMap<Entity.Usuario, UsuarioDto>();

        // DTO -> Entity
        CreateMap<UsuarioCreateDto, Entity.Usuario>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
           .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
           .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
           .ForMember(dest => dest.RefreshTokenExpiry, opt => opt.Ignore())
            .ForMember(dest => dest.Cuentas, opt => opt.Ignore())
            .ForMember(dest => dest.Contacts, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaActualizacion, opt => opt.Ignore());

        CreateMap<UsuarioUpdateDto, Entity.Usuario>()
           .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
           .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
           .ForMember(dest => dest.RefreshTokenExpiry, opt => opt.Ignore())
            .ForMember(dest => dest.Cuentas, opt => opt.Ignore())
            .ForMember(dest => dest.Contacts, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaActualizacion, opt => opt.Ignore());
    }
}
