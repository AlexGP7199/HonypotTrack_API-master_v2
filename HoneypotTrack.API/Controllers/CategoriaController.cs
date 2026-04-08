using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Categoria;
using HonypotTrack.Application.Interfaces;

namespace HoneypotTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // 🔐 Requiere autenticación JWT
public class CategoriaController(ICategoriaService categoriaService) : ControllerBase
{
    private readonly ICategoriaService _categoriaService = categoriaService;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CategoriaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResponse<CategoriaDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] CategoriaFilters filters)
    {
        var response = await _categoriaService.GetAllAsync(filters);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpGet("all")]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<CategoriaDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<CategoriaDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllWithoutPagination()
    {
        var response = await _categoriaService.GetAllWithoutPaginationAsync();

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BaseResponse<CategoriaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<CategoriaDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _categoriaService.GetByIdAsync(id);

        return response.IsSuccess
            ? Ok(response)
            : NotFound(response);
    }

    [HttpGet("tipo/{operationType}")]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<CategoriaDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<IEnumerable<CategoriaDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByOperationType(string operationType)
    {
        var response = await _categoriaService.GetByOperationTypeAsync(operationType);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BaseResponse<CategoriaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseResponse<CategoriaDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CategoriaCreateDto dto)
    {
        var response = await _categoriaService.CreateAsync(dto);

        return response.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = response.Data?.CategoryId }, response)
            : BadRequest(response);
    }

    [HttpPut]
    [ProducesResponseType(typeof(BaseResponse<CategoriaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<CategoriaDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromBody] CategoriaUpdateDto dto)
    {
        var response = await _categoriaService.UpdateAsync(dto);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _categoriaService.DeleteAsync(id);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }
}
