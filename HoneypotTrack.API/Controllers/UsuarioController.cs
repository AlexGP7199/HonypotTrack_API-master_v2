using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Usuario;
using HonypotTrack.Application.Interfaces;

namespace HoneypotTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // 🔐 Requiere autenticación JWT
public class UsuarioController(IUsuarioService usuarioService) : ControllerBase
{
    private readonly IUsuarioService _usuarioService = usuarioService;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResponse<UsuarioDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] UsuarioFilters filters)
    {
        var response = await _usuarioService.GetAllAsync(filters);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpGet("all")]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<UsuarioDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<UsuarioDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllWithoutPagination()
    {
        var response = await _usuarioService.GetAllWithoutPaginationAsync();

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BaseResponse<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<UsuarioDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _usuarioService.GetByIdAsync(id);

        return response.IsSuccess
            ? Ok(response)
            : NotFound(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BaseResponse<UsuarioDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseResponse<UsuarioDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] UsuarioCreateDto dto)
    {
        var response = await _usuarioService.CreateAsync(dto);

        return response.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = response.Data?.UserId }, response)
            : BadRequest(response);
    }

    [HttpPut]
    [ProducesResponseType(typeof(BaseResponse<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<UsuarioDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromBody] UsuarioUpdateDto dto)
    {
        var response = await _usuarioService.UpdateAsync(dto);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _usuarioService.DeleteAsync(id);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpGet("exists-email/{email}")]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExistsByEmail(string email)
    {
        var response = await _usuarioService.ExistsByEmailAsync(email);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }
}
