using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Usuario;
using HonypotTrack.Application.Helpers;
using HonypotTrack.Application.Interfaces;
using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Interfaces;
using System.Linq.Dynamic.Core;

namespace HonypotTrack.Application.Services;

public class UsuarioService(IUnitOfWork unitOfWork) : IUsuarioService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<BaseResponse<PagedResponse<UsuarioDto>>> GetAllAsync(UsuarioFilters filters)
    {
        try
        {
            var query = _unitOfWork.Usuarios.GetQueryable();

            // Filtros
            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                query = query.Where(u =>
                    (u.FullName != null && u.FullName.Contains(filters.Search)) ||
                    u.Email.Contains(filters.Search));
            }

            if (!string.IsNullOrWhiteSpace(filters.FullName))
            {
                query = query.Where(u => u.FullName != null && u.FullName.Contains(filters.FullName));
            }

            if (!string.IsNullOrWhiteSpace(filters.Email))
            {
                query = query.Where(u => u.Email.Contains(filters.Email));
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
                query = query.OrderBy(u => u.UserId);
            }

            // Paginación
            var items = query
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .ToList();

            var itemsDto = AutoMapperHelper.MapList<Usuario, UsuarioDto>(items);
            var pagedResponse = PagedResponse<UsuarioDto>.Create(itemsDto, filters.PageNumber, filters.PageSize, totalRecords);

            return BaseResponse<PagedResponse<UsuarioDto>>.Success(pagedResponse);
        }
        catch (Exception ex)
        {
            return BaseResponse<PagedResponse<UsuarioDto>>.Fail($"Error al obtener usuarios: {ex.Message}");
        }
    }

    public async Task<BaseResponse<IEnumerable<UsuarioDto>>> GetAllWithoutPaginationAsync()
    {
        try
        {
            var usuarios = await _unitOfWork.Usuarios.GetAllAsync();
            var usuariosDto = AutoMapperHelper.MapList<Usuario, UsuarioDto>(usuarios);

            return BaseResponse<IEnumerable<UsuarioDto>>.Success(usuariosDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<UsuarioDto>>.Fail($"Error al obtener usuarios: {ex.Message}");
        }
    }

    public async Task<BaseResponse<UsuarioDto>> GetByIdAsync(int id)
    {
        try
        {
            var usuario = await _unitOfWork.Usuarios.GetByIdAsync(id);

            if (usuario is null)
            {
                return BaseResponse<UsuarioDto>.Fail("Usuario no encontrado");
            }

            var usuarioDto = AutoMapperHelper.Map<Usuario, UsuarioDto>(usuario);

            return BaseResponse<UsuarioDto>.Success(usuarioDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<UsuarioDto>.Fail($"Error al obtener usuario: {ex.Message}");
        }
    }

    public async Task<BaseResponse<UsuarioDto>> CreateAsync(UsuarioCreateDto dto)
    {
        try
        {
            // Validar email único
            var emailExists = await _unitOfWork.Usuarios.ExistsAsync(u => u.Email == dto.Email);
            if (emailExists)
            {
                return BaseResponse<UsuarioDto>.Fail("El email ya está registrado");
            }

            var usuario = AutoMapperHelper.Map<UsuarioCreateDto, Usuario>(dto);

            await _unitOfWork.Usuarios.AddAsync(usuario);
            await _unitOfWork.SaveChangesAsync();

            var usuarioDto = AutoMapperHelper.Map<Usuario, UsuarioDto>(usuario);

            return BaseResponse<UsuarioDto>.Success(usuarioDto, "Usuario creado exitosamente");
        }
        catch (Exception ex)
        {
            return BaseResponse<UsuarioDto>.Fail($"Error al crear usuario: {ex.Message}");
        }
    }

    public async Task<BaseResponse<UsuarioDto>> UpdateAsync(UsuarioUpdateDto dto)
    {
        try
        {
            var usuario = await _unitOfWork.Usuarios.GetByIdAsync(dto.UserId);

            if (usuario is null)
            {
                return BaseResponse<UsuarioDto>.Fail("Usuario no encontrado");
            }

            // Validar email único (excluyendo el actual)
            var emailExists = await _unitOfWork.Usuarios.ExistsAsync(u =>
                u.Email == dto.Email && u.UserId != dto.UserId);

            if (emailExists)
            {
                return BaseResponse<UsuarioDto>.Fail("El email ya está registrado por otro usuario");
            }

            usuario.FullName = dto.FullName;
            usuario.Email = dto.Email;

            _unitOfWork.Usuarios.Update(usuario);
            await _unitOfWork.SaveChangesAsync();

            var usuarioDto = AutoMapperHelper.Map<Usuario, UsuarioDto>(usuario);

            return BaseResponse<UsuarioDto>.Success(usuarioDto, "Usuario actualizado exitosamente");
        }
        catch (Exception ex)
        {
            return BaseResponse<UsuarioDto>.Fail($"Error al actualizar usuario: {ex.Message}");
        }
    }

    public async Task<BaseResponse<bool>> DeleteAsync(int id)
    {
        try
        {
            var usuario = await _unitOfWork.Usuarios.GetByIdAsync(id);

            if (usuario is null)
            {
                return BaseResponse<bool>.Fail("Usuario no encontrado");
            }

            // Validar que no tenga cuentas o contactos asociados
            var tieneCuentas = await _unitOfWork.Cuentas.ExistsAsync(c => c.UserId == id);
            var tieneContactos = await _unitOfWork.Contacts.ExistsAsync(c => c.UserId == id);

            if (tieneCuentas || tieneContactos)
            {
                return BaseResponse<bool>.Fail("No se puede eliminar el usuario porque tiene cuentas o contactos asociados");
            }

            _unitOfWork.Usuarios.Remove(usuario);
            await _unitOfWork.SaveChangesAsync();

            return BaseResponse<bool>.Success(true, "Usuario eliminado exitosamente");
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail($"Error al eliminar usuario: {ex.Message}");
        }
    }

    public async Task<BaseResponse<bool>> ExistsByEmailAsync(string email)
    {
        try
        {
            var exists = await _unitOfWork.Usuarios.ExistsAsync(u => u.Email == email);

            return BaseResponse<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail($"Error al verificar email: {ex.Message}");
        }
    }
}
