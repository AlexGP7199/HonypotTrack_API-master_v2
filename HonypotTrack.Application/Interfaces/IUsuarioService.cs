using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Usuario;

namespace HonypotTrack.Application.Interfaces;

public interface IUsuarioService
{
    // Queries
    Task<BaseResponse<PagedResponse<UsuarioDto>>> GetAllAsync(UsuarioFilters filters);
    Task<BaseResponse<UsuarioDto>> GetByIdAsync(int id);
    Task<BaseResponse<IEnumerable<UsuarioDto>>> GetAllWithoutPaginationAsync();

    // Commands
    Task<BaseResponse<UsuarioDto>> CreateAsync(UsuarioCreateDto dto);
    Task<BaseResponse<UsuarioDto>> UpdateAsync(UsuarioUpdateDto dto);
    Task<BaseResponse<bool>> DeleteAsync(int id);

    // Validations
    Task<BaseResponse<bool>> ExistsByEmailAsync(string email);
}
