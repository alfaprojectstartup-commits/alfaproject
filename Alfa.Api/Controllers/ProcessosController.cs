using System.Collections.Generic;
using Alfa.Api.Aplicacao.Interfaces;
using Alfa.Api.Dtos;
using Alfa.Api.Dtos.Comuns;
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
    public async Task<ActionResult<PaginadoResultadoDto<ProcessoListItemDto>>> Listar([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null)
    {
        var emp = EmpresaId; if (emp <= 0) return BadRequest("EmpresaId ausente");
        var (total, items) = await _app.Listar(emp, page, pageSize, status);
        return Ok(new PaginadoResultadoDto<ProcessoListItemDto>(total, items));
    }

    [HttpGet("padroes")]
    public async Task<ActionResult<IEnumerable<ProcessoPadraoModeloDto>>> ListarPadroes()
    {
        var emp = EmpresaId; if (emp <= 0) return BadRequest("EmpresaId ausente");
        var padroes = await _app.ListarPadroesAsync(emp);
        return Ok(padroes);
    }

    [HttpPost("padroes")]
    public async Task<ActionResult> CriarPadrao([FromBody] ProcessoPadraoModeloInputDto dto)
    {
        var emp = EmpresaId; if (emp <= 0) return BadRequest("EmpresaId ausente");
        var id = await _app.CriarPadraoAsync(emp, dto);
        return CreatedAtAction(nameof(ListarPadroes), new { id }, new { id });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProcessoDetalheDto>> Obter(int id)
    {
        var emp = EmpresaId; if (emp <= 0) return BadRequest("EmpresaId ausente");
        var dto = await _app.Obter(emp, id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult> Criar([FromBody] ProcessoCriarDto dto)
    {
        var emp = EmpresaId; if (emp <= 0) return BadRequest("EmpresaId ausente");
        var id = await _app.Criar(emp, dto);
        return CreatedAtAction(nameof(Obter), new { id }, new { id });
    }

    [HttpGet("{id:int}/fases")]
    public async Task<ActionResult<IEnumerable<FaseInstanciaDto>>> ListarFases(int id)
    {
        var emp = EmpresaId; if (emp <= 0) return BadRequest("EmpresaId ausente");
        var fases = await _app.ListarFases(emp, id);
        return Ok(fases);
    }

    [HttpPost("{id:int}/respostas")]
    public async Task<ActionResult> RegistrarResposta(int id, [FromBody] PaginaRespostaDto dto)
    {
        try
        {
            await _app.RegistrarResposta(id, dto);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
