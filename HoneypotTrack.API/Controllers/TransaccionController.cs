using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Transaccion;
using HonypotTrack.Application.Interfaces;

namespace HoneypotTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // 🔐 Requiere autenticación JWT
public class TransaccionController(ITransaccionService transaccionService) : ControllerBase
{
    private readonly ITransaccionService _transaccionService = transaccionService;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TransaccionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResponse<TransaccionDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] TransaccionFilters filters)
    {
        var response = await _transaccionService.GetAllAsync(filters);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BaseResponse<TransaccionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<TransaccionDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _transaccionService.GetByIdAsync(id);

        return response.IsSuccess
            ? Ok(response)
            : NotFound(response);
    }

    [HttpGet("cuenta/{accountId}")]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<TransaccionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<TransaccionDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByAccountId(int accountId)
    {
        var response = await _transaccionService.GetByAccountIdAsync(accountId);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpGet("categoria/{categoryId}")]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<TransaccionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<TransaccionDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByCategoryId(int categoryId)
    {
        var response = await _transaccionService.GetByCategoryIdAsync(categoryId);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BaseResponse<TransaccionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseResponse<TransaccionDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] TransaccionCreateDto dto)
    {
        var response = await _transaccionService.CreateAsync(dto);

        return response.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = response.Data?.TransaccionId }, response)
            : BadRequest(response);
    }

    [HttpPut]
    [ProducesResponseType(typeof(BaseResponse<TransaccionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<TransaccionDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromBody] TransaccionUpdateDto dto)
    {
        var response = await _transaccionService.UpdateAsync(dto);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _transaccionService.DeleteAsync(id);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpGet("cuenta/{accountId}/total/{operationType}")]
    [ProducesResponseType(typeof(BaseResponse<decimal>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<decimal>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTotalByOperationType(int accountId, string operationType)
    {
        var response = await _transaccionService.GetTotalByOperationTypeAsync(accountId, operationType);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpGet("cuenta/{accountId}/balance")]
    [ProducesResponseType(typeof(BaseResponse<decimal>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<decimal>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBalance(int accountId)
    {
        var response = await _transaccionService.GetBalanceAsync(accountId);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }
}
