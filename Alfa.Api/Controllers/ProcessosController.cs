using Alfa.Api.Aplicacao.Interfaces;
using Alfa.Api.Dtos.Comuns;
using Alfa.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProcessosController : ControllerBase
{
    private readonly IProcessoApp _app;
    public ProcessosController(IProcessoApp app) => _app = app;
    private int EmpresaId =>
    HttpContext.Items.TryGetValue("EmpresaId", out var v)
    && int.TryParse(v?.ToString(), out var id)
        ? id
        : 0;

    [HttpGet]
    public async Task<ActionResult<PaginadoResultDto<ProcessoListItemDto>>> Listar([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null)
    {
        var emp = EmpresaId; if (emp <= 0) return BadRequest("EmpresaId ausente");
        var (total, items) = await _app.Listar(emp, page, pageSize, status);
        return Ok(new PaginadoResultDto<ProcessoListItemDto>(total, items));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProcessoDetalheDto>> Obter(int id)
    {
        var emp = EmpresaId; if (emp <= 0) return BadRequest("EmpresaId ausente");
        var dto = await _app.Obter(emp, id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult> Criar([FromBody] ProcessoCreateDto dto)
    {
        var emp = EmpresaId; if (emp <= 0) return BadRequest("EmpresaId ausente");
        var id = await _app.Criar(emp, dto);
        return CreatedAtAction(nameof(Obter), new { id }, new { id });
    }
}
