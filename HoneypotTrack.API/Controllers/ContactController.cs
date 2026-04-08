using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Contact;
using HonypotTrack.Application.Interfaces;

namespace HoneypotTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // 🔐 Requiere autenticación JWT
public class ContactController(IContactService contactService) : ControllerBase
{
    private readonly IContactService _contactService = contactService;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResponse<ContactDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] ContactFilters filters)
    {
        var response = await _contactService.GetAllAsync(filters);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BaseResponse<ContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<ContactDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _contactService.GetByIdAsync(id);

        return response.IsSuccess
            ? Ok(response)
            : NotFound(response);
    }

    [HttpGet("usuario/{userId}")]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<ContactDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<ContactDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var response = await _contactService.GetByUserIdAsync(userId);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpGet("tipo/{type}")]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<ContactDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<ContactDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByType(string type)
    {
        var response = await _contactService.GetByTypeAsync(type);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BaseResponse<ContactDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseResponse<ContactDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ContactCreateDto dto)
    {
        var response = await _contactService.CreateAsync(dto);

        return response.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = response.Data?.ContactsId }, response)
            : BadRequest(response);
    }

    [HttpPut]
    [ProducesResponseType(typeof(BaseResponse<ContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<ContactDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromBody] ContactUpdateDto dto)
    {
        var response = await _contactService.UpdateAsync(dto);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _contactService.DeleteAsync(id);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }
}
