using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Cuenta;
using HonypotTrack.Application.Helpers;
using HonypotTrack.Application.Interfaces;
using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Interfaces;
using System.Linq.Dynamic.Core;

namespace HonypotTrack.Application.Services;

public class CuentaService(IUnitOfWork unitOfWork) : ICuentaService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<BaseResponse<PagedResponse<CuentaDto>>> GetAllAsync(CuentaFilters filters)
    {
        try
        {
            var query = _unitOfWork.Cuentas.GetQueryable();

            // Filtros
            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                query = query.Where(c => c.AccountName.Contains(filters.Search));
            }

            if (filters.UserId.HasValue)
            {
                query = query.Where(c => c.UserId == filters.UserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.AccountName))
            {
                query = query.Where(c => c.AccountName.Contains(filters.AccountName));
            }

            if (!string.IsNullOrWhiteSpace(filters.Currency))
            {
                query = query.Where(c => c.Currency == filters.Currency);
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
                query = query.OrderBy(c => c.AccountId);
            }

            // Paginación
            var items = query
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .ToList();

            var itemsDto = AutoMapperHelper.MapList<Cuenta, CuentaDto>(items);
            var pagedResponse = PagedResponse<CuentaDto>.Create(itemsDto, filters.PageNumber, filters.PageSize, totalRecords);

            return BaseResponse<PagedResponse<CuentaDto>>.Success(pagedResponse);
        }
        catch (Exception ex)
        {
            return BaseResponse<PagedResponse<CuentaDto>>.Fail($"Error al obtener cuentas: {ex.Message}");
        }
    }

    public async Task<BaseResponse<CuentaDto>> GetByIdAsync(int id)
    {
        try
        {
            var cuenta = await _unitOfWork.Cuentas.GetByIdAsync(id);

            if (cuenta is null)
            {
                return BaseResponse<CuentaDto>.Fail("Cuenta no encontrada");
            }

            var cuentaDto = AutoMapperHelper.Map<Cuenta, CuentaDto>(cuenta);

            return BaseResponse<CuentaDto>.Success(cuentaDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<CuentaDto>.Fail($"Error al obtener cuenta: {ex.Message}");
        }
    }

    public async Task<BaseResponse<IEnumerable<CuentaDto>>> GetByUserIdAsync(int userId)
    {
        try
        {
            var cuentas = await _unitOfWork.Cuentas.FindAsync(c => c.UserId == userId);
            var cuentasDto = AutoMapperHelper.MapList<Cuenta, CuentaDto>(cuentas);

            return BaseResponse<IEnumerable<CuentaDto>>.Success(cuentasDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<CuentaDto>>.Fail($"Error al obtener cuentas: {ex.Message}");
        }
    }

    public async Task<BaseResponse<CuentaDto>> CreateAsync(CuentaCreateDto dto)
    {
        try
        {
            // Validar que el usuario exista
            var usuarioExists = await _unitOfWork.Usuarios.ExistsAsync(u => u.UserId == dto.UserId);
            if (!usuarioExists)
            {
                return BaseResponse<CuentaDto>.Fail("El usuario no existe");
            }

            var cuenta = AutoMapperHelper.Map<CuentaCreateDto, Cuenta>(dto);

            await _unitOfWork.Cuentas.AddAsync(cuenta);
            await _unitOfWork.SaveChangesAsync();

            var cuentaDto = AutoMapperHelper.Map<Cuenta, CuentaDto>(cuenta);

            return BaseResponse<CuentaDto>.Success(cuentaDto, "Cuenta creada exitosamente");
        }
        catch (Exception ex)
        {
            return BaseResponse<CuentaDto>.Fail($"Error al crear cuenta: {ex.Message}");
        }
    }

    public async Task<BaseResponse<CuentaDto>> UpdateAsync(CuentaUpdateDto dto)
    {
        try
        {
            var cuenta = await _unitOfWork.Cuentas.GetByIdAsync(dto.AccountId);

            if (cuenta is null)
            {
                return BaseResponse<CuentaDto>.Fail("Cuenta no encontrada");
            }

            // Validar que el usuario exista
            var usuarioExists = await _unitOfWork.Usuarios.ExistsAsync(u => u.UserId == dto.UserId);
            if (!usuarioExists)
            {
                return BaseResponse<CuentaDto>.Fail("El usuario no existe");
            }

            cuenta.UserId = dto.UserId;
            cuenta.AccountName = dto.AccountName;
            cuenta.Currency = dto.Currency;

            _unitOfWork.Cuentas.Update(cuenta);
            await _unitOfWork.SaveChangesAsync();

            var cuentaDto = AutoMapperHelper.Map<Cuenta, CuentaDto>(cuenta);

            return BaseResponse<CuentaDto>.Success(cuentaDto, "Cuenta actualizada exitosamente");
        }
        catch (Exception ex)
        {
            return BaseResponse<CuentaDto>.Fail($"Error al actualizar cuenta: {ex.Message}");
        }
    }

    public async Task<BaseResponse<bool>> DeleteAsync(int id)
    {
        try
        {
            var cuenta = await _unitOfWork.Cuentas.GetByIdAsync(id);

            if (cuenta is null)
            {
                return BaseResponse<bool>.Fail("Cuenta no encontrada");
            }

            // Validar que no tenga transacciones
            var tieneTransacciones = await _unitOfWork.Transacciones.ExistsAsync(t => t.AccountId == id);
            if (tieneTransacciones)
            {
                return BaseResponse<bool>.Fail("No se puede eliminar la cuenta porque tiene transacciones asociadas");
            }

            _unitOfWork.Cuentas.Remove(cuenta);
            await _unitOfWork.SaveChangesAsync();

            return BaseResponse<bool>.Success(true, "Cuenta eliminada exitosamente");
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail($"Error al eliminar cuenta: {ex.Message}");
        }
    }
}
