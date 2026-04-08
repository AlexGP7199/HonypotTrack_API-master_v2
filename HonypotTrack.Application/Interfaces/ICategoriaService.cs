using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Categoria;

namespace HonypotTrack.Application.Interfaces;

public interface ICategoriaService
{
    // Queries
    Task<BaseResponse<PagedResponse<CategoriaDto>>> GetAllAsync(CategoriaFilters filters);
    Task<BaseResponse<CategoriaDto>> GetByIdAsync(int id);
    Task<BaseResponse<IEnumerable<CategoriaDto>>> GetByOperationTypeAsync(string operationType);
    Task<BaseResponse<IEnumerable<CategoriaDto>>> GetAllWithoutPaginationAsync();

    // Commands
    Task<BaseResponse<CategoriaDto>> CreateAsync(CategoriaCreateDto dto);
    Task<BaseResponse<CategoriaDto>> UpdateAsync(CategoriaUpdateDto dto);
    Task<BaseResponse<bool>> DeleteAsync(int id);
}
