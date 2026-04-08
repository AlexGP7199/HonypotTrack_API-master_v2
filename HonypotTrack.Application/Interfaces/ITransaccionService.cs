using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Transaccion;

namespace HonypotTrack.Application.Interfaces;

public interface ITransaccionService
{
    // Queries
    Task<BaseResponse<PagedResponse<TransaccionDto>>> GetAllAsync(TransaccionFilters filters);
    Task<BaseResponse<TransaccionDto>> GetByIdAsync(int id);
    Task<BaseResponse<IEnumerable<TransaccionDto>>> GetByAccountIdAsync(int accountId);
    Task<BaseResponse<IEnumerable<TransaccionDto>>> GetByCategoryIdAsync(int categoryId);

    // Commands
    Task<BaseResponse<TransaccionDto>> CreateAsync(TransaccionCreateDto dto);
    Task<BaseResponse<TransaccionDto>> UpdateAsync(TransaccionUpdateDto dto);
    Task<BaseResponse<bool>> DeleteAsync(int id);

    // Reports
    Task<BaseResponse<decimal>> GetTotalByOperationTypeAsync(int accountId, string operationType);
    Task<BaseResponse<decimal>> GetBalanceAsync(int accountId);
}
