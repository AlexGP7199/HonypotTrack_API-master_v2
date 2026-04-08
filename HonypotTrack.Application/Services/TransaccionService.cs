using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Transaccion;
using HonypotTrack.Application.Helpers;
using HonypotTrack.Application.Interfaces;
using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Interfaces;
using System.Linq.Dynamic.Core;

namespace HonypotTrack.Application.Services;

public class TransaccionService(IUnitOfWork unitOfWork) : ITransaccionService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<BaseResponse<PagedResponse<TransaccionDto>>> GetAllAsync(TransaccionFilters filters)
    {
        try
        {
            var query = _unitOfWork.Transacciones.GetQueryable();

            // Filtros
            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                query = query.Where(t =>
                    (t.Descripcion != null && t.Descripcion.Contains(filters.Search)));
            }

            if (filters.AccountId.HasValue)
            {
                query = query.Where(t => t.AccountId == filters.AccountId.Value);
            }

            if (filters.CategoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == filters.CategoryId.Value);
            }

            if (filters.ContactsId.HasValue)
            {
                query = query.Where(t => t.ContactsId == filters.ContactsId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.Moneda))
            {
                query = query.Where(t => t.Moneda == filters.Moneda);
            }

            if (filters.MontoMinimo.HasValue)
            {
                query = query.Where(t => t.Monto >= filters.MontoMinimo.Value);
            }

            if (filters.MontoMaximo.HasValue)
            {
                query = query.Where(t => t.Monto <= filters.MontoMaximo.Value);
            }

            // Filtro por rango de fechas
            if (filters.StartDate.HasValue)
            {
                query = query.Where(t => t.Fecha >= filters.StartDate.Value);
            }

            if (filters.EndDate.HasValue)
            {
                query = query.Where(t => t.Fecha <= filters.EndDate.Value);
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
                query = query.OrderByDescending(t => t.Fecha);
            }

            // Paginación
            var items = query
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .ToList();

            var itemsDto = AutoMapperHelper.MapList<Transaccion, TransaccionDto>(items);
            var pagedResponse = PagedResponse<TransaccionDto>.Create(itemsDto, filters.PageNumber, filters.PageSize, totalRecords);

            return BaseResponse<PagedResponse<TransaccionDto>>.Success(pagedResponse);
        }
        catch (Exception ex)
        {
            return BaseResponse<PagedResponse<TransaccionDto>>.Fail($"Error al obtener transacciones: {ex.Message}");
        }
    }

    public async Task<BaseResponse<TransaccionDto>> GetByIdAsync(int id)
    {
        try
        {
            var transaccion = await _unitOfWork.Transacciones.GetByIdAsync(id);

            if (transaccion is null)
            {
                return BaseResponse<TransaccionDto>.Fail("Transacción no encontrada");
            }

            var transaccionDto = AutoMapperHelper.Map<Transaccion, TransaccionDto>(transaccion);

            return BaseResponse<TransaccionDto>.Success(transaccionDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<TransaccionDto>.Fail($"Error al obtener transacción: {ex.Message}");
        }
    }

    public async Task<BaseResponse<IEnumerable<TransaccionDto>>> GetByAccountIdAsync(int accountId)
    {
        try
        {
            var transacciones = await _unitOfWork.Transacciones.FindAsync(t => t.AccountId == accountId);
            var transaccionesDto = AutoMapperHelper.MapList<Transaccion, TransaccionDto>(transacciones);

            return BaseResponse<IEnumerable<TransaccionDto>>.Success(transaccionesDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<TransaccionDto>>.Fail($"Error al obtener transacciones: {ex.Message}");
        }
    }

    public async Task<BaseResponse<IEnumerable<TransaccionDto>>> GetByCategoryIdAsync(int categoryId)
    {
        try
        {
            var transacciones = await _unitOfWork.Transacciones.FindAsync(t => t.CategoryId == categoryId);
            var transaccionesDto = AutoMapperHelper.MapList<Transaccion, TransaccionDto>(transacciones);

            return BaseResponse<IEnumerable<TransaccionDto>>.Success(transaccionesDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<TransaccionDto>>.Fail($"Error al obtener transacciones: {ex.Message}");
        }
    }

    public async Task<BaseResponse<TransaccionDto>> CreateAsync(TransaccionCreateDto dto)
    {
        try
        {
            // Validar que la cuenta exista
            var cuentaExists = await _unitOfWork.Cuentas.ExistsAsync(c => c.AccountId == dto.AccountId);
            if (!cuentaExists)
            {
                return BaseResponse<TransaccionDto>.Fail("La cuenta no existe");
            }

            // Validar que la categoría exista
            var categoriaExists = await _unitOfWork.Categorias.ExistsAsync(c => c.CategoryId == dto.CategoryId);
            if (!categoriaExists)
            {
                return BaseResponse<TransaccionDto>.Fail("La categoría no existe");
            }

            // Validar contacto si se proporciona
            if (dto.ContactsId.HasValue)
            {
                var contactExists = await _unitOfWork.Contacts.ExistsAsync(c => c.ContactsId == dto.ContactsId.Value);
                if (!contactExists)
                {
                    return BaseResponse<TransaccionDto>.Fail("El contacto no existe");
                }
            }

            var transaccion = AutoMapperHelper.Map<TransaccionCreateDto, Transaccion>(dto);
            transaccion.Fecha ??= DateTime.Now;

            await _unitOfWork.Transacciones.AddAsync(transaccion);
            await _unitOfWork.SaveChangesAsync();

            var transaccionDto = AutoMapperHelper.Map<Transaccion, TransaccionDto>(transaccion);

            return BaseResponse<TransaccionDto>.Success(transaccionDto, "Transacción creada exitosamente");
        }
        catch (Exception ex)
        {
            return BaseResponse<TransaccionDto>.Fail($"Error al crear transacción: {ex.Message}");
        }
    }

    public async Task<BaseResponse<TransaccionDto>> UpdateAsync(TransaccionUpdateDto dto)
    {
        try
        {
            var transaccion = await _unitOfWork.Transacciones.GetByIdAsync(dto.TransaccionId);

            if (transaccion is null)
            {
                return BaseResponse<TransaccionDto>.Fail("Transacción no encontrada");
            }

            // Validar que la cuenta exista
            var cuentaExists = await _unitOfWork.Cuentas.ExistsAsync(c => c.AccountId == dto.AccountId);
            if (!cuentaExists)
            {
                return BaseResponse<TransaccionDto>.Fail("La cuenta no existe");
            }

            // Validar que la categoría exista
            var categoriaExists = await _unitOfWork.Categorias.ExistsAsync(c => c.CategoryId == dto.CategoryId);
            if (!categoriaExists)
            {
                return BaseResponse<TransaccionDto>.Fail("La categoría no existe");
            }

            // Validar contacto si se proporciona
            if (dto.ContactsId.HasValue)
            {
                var contactExists = await _unitOfWork.Contacts.ExistsAsync(c => c.ContactsId == dto.ContactsId.Value);
                if (!contactExists)
                {
                    return BaseResponse<TransaccionDto>.Fail("El contacto no existe");
                }
            }

            transaccion.AccountId = dto.AccountId;
            transaccion.CategoryId = dto.CategoryId;
            transaccion.ContactsId = dto.ContactsId;
            transaccion.Monto = dto.Monto;
            transaccion.Moneda = dto.Moneda;
            transaccion.Descripcion = dto.Descripcion;
            transaccion.Fecha = dto.Fecha;

            _unitOfWork.Transacciones.Update(transaccion);
            await _unitOfWork.SaveChangesAsync();

            var transaccionDto = AutoMapperHelper.Map<Transaccion, TransaccionDto>(transaccion);

            return BaseResponse<TransaccionDto>.Success(transaccionDto, "Transacción actualizada exitosamente");
        }
        catch (Exception ex)
        {
            return BaseResponse<TransaccionDto>.Fail($"Error al actualizar transacción: {ex.Message}");
        }
    }

    public async Task<BaseResponse<bool>> DeleteAsync(int id)
    {
        try
        {
            var transaccion = await _unitOfWork.Transacciones.GetByIdAsync(id);

            if (transaccion is null)
            {
                return BaseResponse<bool>.Fail("Transacción no encontrada");
            }

            _unitOfWork.Transacciones.Remove(transaccion);
            await _unitOfWork.SaveChangesAsync();

            return BaseResponse<bool>.Success(true, "Transacción eliminada exitosamente");
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail($"Error al eliminar transacción: {ex.Message}");
        }
    }

    public async Task<BaseResponse<decimal>> GetTotalByOperationTypeAsync(int accountId, string operationType)
    {
        try
        {
            var transacciones = _unitOfWork.Transacciones.GetQueryable()
                .Where(t => t.AccountId == accountId)
                .ToList();

            // Obtener categorías para filtrar por tipo de operación
            var categorias = await _unitOfWork.Categorias.FindAsync(c => c.OperationType == operationType);
            var categoryIds = categorias.Select(c => c.CategoryId).ToList();

            var total = transacciones
                .Where(t => categoryIds.Contains(t.CategoryId))
                .Sum(t => t.Monto);

            return BaseResponse<decimal>.Success(total);
        }
        catch (Exception ex)
        {
            return BaseResponse<decimal>.Fail($"Error al calcular total: {ex.Message}");
        }
    }

    public async Task<BaseResponse<decimal>> GetBalanceAsync(int accountId)
    {
        try
        {
            var transacciones = _unitOfWork.Transacciones.GetQueryable()
                .Where(t => t.AccountId == accountId)
                .ToList();

            var categoriasIngreso = await _unitOfWork.Categorias.FindAsync(c => c.OperationType == "Ingreso");
            var categoriaIdsIngreso = categoriasIngreso.Select(c => c.CategoryId).ToList();

            var categoriasEgreso = await _unitOfWork.Categorias.FindAsync(c => c.OperationType == "Egreso");
            var categoriaIdsEgreso = categoriasEgreso.Select(c => c.CategoryId).ToList();

            var ingresos = transacciones
                .Where(t => categoriaIdsIngreso.Contains(t.CategoryId))
                .Sum(t => t.Monto);

            var egresos = transacciones
                .Where(t => categoriaIdsEgreso.Contains(t.CategoryId))
                .Sum(t => t.Monto);

            var balance = ingresos - egresos;

            return BaseResponse<decimal>.Success(balance);
        }
        catch (Exception ex)
        {
            return BaseResponse<decimal>.Fail($"Error al calcular balance: {ex.Message}");
        }
    }
}
