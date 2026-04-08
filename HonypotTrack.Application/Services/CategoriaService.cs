using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Categoria;
using HonypotTrack.Application.Helpers;
using HonypotTrack.Application.Interfaces;
using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Interfaces;
using System.Linq.Dynamic.Core;

namespace HonypotTrack.Application.Services;

public class CategoriaService(IUnitOfWork unitOfWork) : ICategoriaService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<BaseResponse<PagedResponse<CategoriaDto>>> GetAllAsync(CategoriaFilters filters)
    {
        try
        {
            var query = _unitOfWork.Categorias.GetQueryable();

            // Filtros
            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                query = query.Where(c => c.Name.Contains(filters.Search));
            }

            if (!string.IsNullOrWhiteSpace(filters.Name))
            {
                query = query.Where(c => c.Name.Contains(filters.Name));
            }

            if (!string.IsNullOrWhiteSpace(filters.OperationType))
            {
                query = query.Where(c => c.OperationType == filters.OperationType);
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
                query = query.OrderBy(c => c.CategoryId);
            }

            // Paginación
            var items = query
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .ToList();

            var itemsDto = AutoMapperHelper.MapList<Categoria, CategoriaDto>(items);
            var pagedResponse = PagedResponse<CategoriaDto>.Create(itemsDto, filters.PageNumber, filters.PageSize, totalRecords);

            return BaseResponse<PagedResponse<CategoriaDto>>.Success(pagedResponse);
        }
        catch (Exception ex)
        {
            return BaseResponse<PagedResponse<CategoriaDto>>.Fail($"Error al obtener categorías: {ex.Message}");
        }
    }

    public async Task<BaseResponse<IEnumerable<CategoriaDto>>> GetAllWithoutPaginationAsync()
    {
        try
        {
            var categorias = await _unitOfWork.Categorias.GetAllAsync();
            var categoriasDto = AutoMapperHelper.MapList<Categoria, CategoriaDto>(categorias);

            return BaseResponse<IEnumerable<CategoriaDto>>.Success(categoriasDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<CategoriaDto>>.Fail($"Error al obtener categorías: {ex.Message}");
        }
    }

    public async Task<BaseResponse<CategoriaDto>> GetByIdAsync(int id)
    {
        try
        {
            var categoria = await _unitOfWork.Categorias.GetByIdAsync(id);

            if (categoria is null)
            {
                return BaseResponse<CategoriaDto>.Fail("Categoría no encontrada");
            }

            var categoriaDto = AutoMapperHelper.Map<Categoria, CategoriaDto>(categoria);

            return BaseResponse<CategoriaDto>.Success(categoriaDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<CategoriaDto>.Fail($"Error al obtener categoría: {ex.Message}");
        }
    }

    public async Task<BaseResponse<IEnumerable<CategoriaDto>>> GetByOperationTypeAsync(string operationType)
    {
        try
        {
            var categorias = await _unitOfWork.Categorias.FindAsync(c => c.OperationType == operationType);
            var categoriasDto = AutoMapperHelper.MapList<Categoria, CategoriaDto>(categorias);

            return BaseResponse<IEnumerable<CategoriaDto>>.Success(categoriasDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<CategoriaDto>>.Fail($"Error al obtener categorías: {ex.Message}");
        }
    }

    public async Task<BaseResponse<CategoriaDto>> CreateAsync(CategoriaCreateDto dto)
    {
        try
        {
            // Validar tipo de operación
            if (dto.OperationType != "Ingreso" && dto.OperationType != "Egreso")
            {
                return BaseResponse<CategoriaDto>.Fail("El tipo de operación debe ser 'Ingreso' o 'Egreso'");
            }

            var categoria = AutoMapperHelper.Map<CategoriaCreateDto, Categoria>(dto);

            await _unitOfWork.Categorias.AddAsync(categoria);
            await _unitOfWork.SaveChangesAsync();

            var categoriaDto = AutoMapperHelper.Map<Categoria, CategoriaDto>(categoria);

            return BaseResponse<CategoriaDto>.Success(categoriaDto, "Categoría creada exitosamente");
        }
        catch (Exception ex)
        {
            return BaseResponse<CategoriaDto>.Fail($"Error al crear categoría: {ex.Message}");
        }
    }

    public async Task<BaseResponse<CategoriaDto>> UpdateAsync(CategoriaUpdateDto dto)
    {
        try
        {
            var categoria = await _unitOfWork.Categorias.GetByIdAsync(dto.CategoryId);

            if (categoria is null)
            {
                return BaseResponse<CategoriaDto>.Fail("Categoría no encontrada");
            }

            // Validar tipo de operación
            if (dto.OperationType != "Ingreso" && dto.OperationType != "Egreso")
            {
                return BaseResponse<CategoriaDto>.Fail("El tipo de operación debe ser 'Ingreso' o 'Egreso'");
            }

            categoria.Name = dto.Name;
            categoria.OperationType = dto.OperationType;

            _unitOfWork.Categorias.Update(categoria);
            await _unitOfWork.SaveChangesAsync();

            var categoriaDto = AutoMapperHelper.Map<Categoria, CategoriaDto>(categoria);

            return BaseResponse<CategoriaDto>.Success(categoriaDto, "Categoría actualizada exitosamente");
        }
        catch (Exception ex)
        {
            return BaseResponse<CategoriaDto>.Fail($"Error al actualizar categoría: {ex.Message}");
        }
    }

    public async Task<BaseResponse<bool>> DeleteAsync(int id)
    {
        try
        {
            var categoria = await _unitOfWork.Categorias.GetByIdAsync(id);

            if (categoria is null)
            {
                return BaseResponse<bool>.Fail("Categoría no encontrada");
            }

            // Validar que no tenga transacciones
            var tieneTransacciones = await _unitOfWork.Transacciones.ExistsAsync(t => t.CategoryId == id);
            if (tieneTransacciones)
            {
                return BaseResponse<bool>.Fail("No se puede eliminar la categoría porque tiene transacciones asociadas");
            }

            _unitOfWork.Categorias.Remove(categoria);
            await _unitOfWork.SaveChangesAsync();

            return BaseResponse<bool>.Success(true, "Categoría eliminada exitosamente");
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail($"Error al eliminar categoría: {ex.Message}");
        }
    }
}
