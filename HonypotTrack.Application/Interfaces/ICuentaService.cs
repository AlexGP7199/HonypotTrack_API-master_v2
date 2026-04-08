using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Cuenta;

namespace HonypotTrack.Application.Interfaces;

public interface ICuentaService
{
    // Queries
    Task<BaseResponse<PagedResponse<CuentaDto>>> GetAllAsync(CuentaFilters filters);
    Task<BaseResponse<CuentaDto>> GetByIdAsync(int id);
    Task<BaseResponse<IEnumerable<CuentaDto>>> GetByUserIdAsync(int userId);

    // Commands
    Task<BaseResponse<CuentaDto>> CreateAsync(CuentaCreateDto dto);
    Task<BaseResponse<CuentaDto>> UpdateAsync(CuentaUpdateDto dto);
    Task<BaseResponse<bool>> DeleteAsync(int id);
}
