using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Contact;
using HonypotTrack.Application.Helpers;
using HonypotTrack.Application.Interfaces;
using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Interfaces;
using System.Linq.Dynamic.Core;

namespace HonypotTrack.Application.Services;

public class ContactService(IUnitOfWork unitOfWork) : IContactService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<BaseResponse<PagedResponse<ContactDto>>> GetAllAsync(ContactFilters filters)
    {
        try
        {
            var query = _unitOfWork.Contacts.GetQueryable();

            // Filtros
            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                query = query.Where(c =>
                    c.Name.Contains(filters.Search) ||
                    c.TaxId.Contains(filters.Search));
            }

            if (filters.UserId.HasValue)
            {
                query = query.Where(c => c.UserId == filters.UserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.Name))
            {
                query = query.Where(c => c.Name.Contains(filters.Name));
            }

            if (!string.IsNullOrWhiteSpace(filters.Type))
            {
                query = query.Where(c => c.Type == filters.Type);
            }

            if (!string.IsNullOrWhiteSpace(filters.TaxId))
            {
                query = query.Where(c => c.TaxId.Contains(filters.TaxId));
            }

            // Total de registros
            var totalRecords = query.Count();

            // Ordenamiento
            if (!string.IsNullOrWhiteSpace(filters.OrderBy))
            {
                var orderDirection = filters.IsDescending ? "descending" : "ascending";
                query = query.OrderBy($"{filters.OrderBy} {orderDirection}");
            }
            else
            {
                query = query.OrderBy(c => c.ContactsId);
            }

            // Paginación
            var items = query
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .ToList();

            var itemsDto = AutoMapperHelper.MapList<Contact, ContactDto>(items);
            var pagedResponse = PagedResponse<ContactDto>.Create(itemsDto, filters.PageNumber, filters.PageSize, totalRecords);

            return BaseResponse<PagedResponse<ContactDto>>.Success(pagedResponse);
        }
        catch (Exception ex)
        {
            return BaseResponse<PagedResponse<ContactDto>>.Fail($"Error al obtener contactos: {ex.Message}");
        }
    }

    public async Task<BaseResponse<ContactDto>> GetByIdAsync(int id)
    {
        try
        {
            var contact = await _unitOfWork.Contacts.GetByIdAsync(id);

            if (contact is null)
            {
                return BaseResponse<ContactDto>.Fail("Contacto no encontrado");
            }

            var contactDto = AutoMapperHelper.Map<Contact, ContactDto>(contact);

            return BaseResponse<ContactDto>.Success(contactDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<ContactDto>.Fail($"Error al obtener contacto: {ex.Message}");
        }
    }

    public async Task<BaseResponse<IEnumerable<ContactDto>>> GetByUserIdAsync(int userId)
    {
        try
        {
            var contacts = await _unitOfWork.Contacts.FindAsync(c => c.UserId == userId);
            var contactsDto = AutoMapperHelper.MapList<Contact, ContactDto>(contacts);

            return BaseResponse<IEnumerable<ContactDto>>.Success(contactsDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<ContactDto>>.Fail($"Error al obtener contactos: {ex.Message}");
        }
    }

    public async Task<BaseResponse<IEnumerable<ContactDto>>> GetByTypeAsync(string type)
    {
        try
        {
            var contacts = await _unitOfWork.Contacts.FindAsync(c => c.Type == type);
            var contactsDto = AutoMapperHelper.MapList<Contact, ContactDto>(contacts);

            return BaseResponse<IEnumerable<ContactDto>>.Success(contactsDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<ContactDto>>.Fail($"Error al obtener contactos: {ex.Message}");
        }
    }

    public async Task<BaseResponse<ContactDto>> CreateAsync(ContactCreateDto dto)
    {
        try
        {
            // Validar que el usuario exista
            var usuarioExists = await _unitOfWork.Usuarios.ExistsAsync(u => u.UserId == dto.UserId);
            if (!usuarioExists)
            {
                return BaseResponse<ContactDto>.Fail("El usuario no existe");
            }

            var contact = AutoMapperHelper.Map<ContactCreateDto, Contact>(dto);

            await _unitOfWork.Contacts.AddAsync(contact);
            await _unitOfWork.SaveChangesAsync();

            var contactDto = AutoMapperHelper.Map<Contact, ContactDto>(contact);

            return BaseResponse<ContactDto>.Success(contactDto, "Contacto creado exitosamente");
        }
        catch (Exception ex)
        {
            return BaseResponse<ContactDto>.Fail($"Error al crear contacto: {ex.Message}");
        }
    }

    public async Task<BaseResponse<ContactDto>> UpdateAsync(ContactUpdateDto dto)
    {
        try
        {
            var contact = await _unitOfWork.Contacts.GetByIdAsync(dto.ContactsId);

            if (contact is null)
            {
                return BaseResponse<ContactDto>.Fail("Contacto no encontrado");
            }

            // Validar que el usuario exista
            var usuarioExists = await _unitOfWork.Usuarios.ExistsAsync(u => u.UserId == dto.UserId);
            if (!usuarioExists)
            {
                return BaseResponse<ContactDto>.Fail("El usuario no existe");
            }

            contact.UserId = dto.UserId;
            contact.Name = dto.Name;
            contact.Type = dto.Type;
            contact.TaxId = dto.TaxId;

            _unitOfWork.Contacts.Update(contact);
            await _unitOfWork.SaveChangesAsync();

            var contactDto = AutoMapperHelper.Map<Contact, ContactDto>(contact);

            return BaseResponse<ContactDto>.Success(contactDto, "Contacto actualizado exitosamente");
        }
        catch (Exception ex)
        {
            return BaseResponse<ContactDto>.Fail($"Error al actualizar contacto: {ex.Message}");
        }
    }

    public async Task<BaseResponse<bool>> DeleteAsync(int id)
    {
        try
        {
            var contact = await _unitOfWork.Contacts.GetByIdAsync(id);

            if (contact is null)
            {
                return BaseResponse<bool>.Fail("Contacto no encontrado");
            }

            // Validar que no tenga transacciones
            var tieneTransacciones = await _unitOfWork.Transacciones.ExistsAsync(t => t.ContactsId == id);
            if (tieneTransacciones)
            {
                return BaseResponse<bool>.Fail("No se puede eliminar el contacto porque tiene transacciones asociadas");
            }

            _unitOfWork.Contacts.Remove(contact);
            await _unitOfWork.SaveChangesAsync();

            return BaseResponse<bool>.Success(true, "Contacto eliminado exitosamente");
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail($"Error al eliminar contacto: {ex.Message}");
        }
    }
}
