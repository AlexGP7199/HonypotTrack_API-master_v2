using AutoMapper;
using HonypotTrack.Application.Dtos.Transaccion;
using Entity = HoneypotTrack.Domain.Entities;

namespace HonypotTrack.Application.MapperProfiles;

public class TransaccionProfile : Profile
{
    public TransaccionProfile()
    {
        // Entity -> DTO
        CreateMap<Entity.Transaccion, TransaccionDto>()
            .ForMember(dest => dest.CuentaNombre, opt => opt.MapFrom(src => src.Cuenta != null ? src.Cuenta.AccountName : null))
            .ForMember(dest => dest.CategoriaNombre, opt => opt.MapFrom(src => src.Categoria != null ? src.Categoria.Name : null))
            .ForMember(dest => dest.ContactoNombre, opt => opt.MapFrom(src => src.Contact != null ? src.Contact.Name : null))
            .ForMember(dest => dest.TipoOperacion, opt => opt.MapFrom(src => src.Categoria != null ? src.Categoria.OperationType : null));

        // DTO -> Entity
        CreateMap<TransaccionCreateDto, Entity.Transaccion>()
            .ForMember(dest => dest.TransaccionId, opt => opt.Ignore())
            .ForMember(dest => dest.Cuenta, opt => opt.Ignore())
            .ForMember(dest => dest.Categoria, opt => opt.Ignore())
            .ForMember(dest => dest.Contact, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaActualizacion, opt => opt.Ignore());

        CreateMap<TransaccionUpdateDto, Entity.Transaccion>()
            .ForMember(dest => dest.Cuenta, opt => opt.Ignore())
            .ForMember(dest => dest.Categoria, opt => opt.Ignore())
            .ForMember(dest => dest.Contact, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaActualizacion, opt => opt.Ignore());
    }
}
