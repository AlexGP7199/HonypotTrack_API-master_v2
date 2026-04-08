using AutoMapper;
using HonypotTrack.Application.Dtos.Contact;
using Entity = HoneypotTrack.Domain.Entities;

namespace HonypotTrack.Application.MapperProfiles;

public class ContactProfile : Profile
{
    public ContactProfile()
    {
        // Entity -> DTO
        CreateMap<Entity.Contact, ContactDto>()
            .ForMember(dest => dest.UsuarioNombre, opt => opt.MapFrom(src => src.Usuario != null ? src.Usuario.FullName : null));

        // DTO -> Entity
        CreateMap<ContactCreateDto, Entity.Contact>()
            .ForMember(dest => dest.ContactsId, opt => opt.Ignore())
            .ForMember(dest => dest.Usuario, opt => opt.Ignore())
            .ForMember(dest => dest.Transacciones, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaActualizacion, opt => opt.Ignore());

        CreateMap<ContactUpdateDto, Entity.Contact>()
            .ForMember(dest => dest.Usuario, opt => opt.Ignore())
            .ForMember(dest => dest.Transacciones, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaActualizacion, opt => opt.Ignore());
    }
}
