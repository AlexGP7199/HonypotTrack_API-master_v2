using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Cuenta;
using HonypotTrack.Application.Interfaces;

namespace HoneypotTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // 🔐 Requiere autenticación JWT
public class CuentaController(ICuentaService cuentaService) : ControllerBase
{
    private readonly ICuentaService _cuentaService = cuentaService;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CuentaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResponse<CuentaDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] CuentaFilters filters)
    {
        var response = await _cuentaService.GetAllAsync(filters);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BaseResponse<CuentaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<CuentaDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _cuentaService.GetByIdAsync(id);

        return response.IsSuccess
            ? Ok(response)
            : NotFound(response);
    }

    [HttpGet("usuario/{userId}")]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<CuentaDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<CuentaDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var response = await _cuentaService.GetByUserIdAsync(userId);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BaseResponse<CuentaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseResponse<CuentaDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CuentaCreateDto dto)
    {
        var response = await _cuentaService.CreateAsync(dto);

        return response.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = response.Data?.AccountId }, response)
            : BadRequest(response);
    }

    [HttpPut]
    [ProducesResponseType(typeof(BaseResponse<CuentaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<CuentaDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromBody] CuentaUpdateDto dto)
    {
        var response = await _cuentaService.UpdateAsync(dto);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _cuentaService.DeleteAsync(id);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }
}
