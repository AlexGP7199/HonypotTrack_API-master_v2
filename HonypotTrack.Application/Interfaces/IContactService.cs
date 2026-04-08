using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Contact;

namespace HonypotTrack.Application.Interfaces;

public interface IContactService
{
    // Queries
    Task<BaseResponse<PagedResponse<ContactDto>>> GetAllAsync(ContactFilters filters);
    Task<BaseResponse<ContactDto>> GetByIdAsync(int id);
    Task<BaseResponse<IEnumerable<ContactDto>>> GetByUserIdAsync(int userId);
    Task<BaseResponse<IEnumerable<ContactDto>>> GetByTypeAsync(string type);

    // Commands
    Task<BaseResponse<ContactDto>> CreateAsync(ContactCreateDto dto);
    Task<BaseResponse<ContactDto>> UpdateAsync(ContactUpdateDto dto);
    Task<BaseResponse<bool>> DeleteAsync(int id);
}
