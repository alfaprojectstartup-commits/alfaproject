using System.Collections.Generic;
using Alfa.Api.Aplicacao.Interfaces;
using Alfa.Api.Dtos;
using Alfa.Api.Dtos.Comuns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProcessosController : ControllerBase
{
    private readonly IProcessoApp _app;
    public ProcessosController(IProcessoApp app) => _app = app;

    private int EmpresaId
    {
        get
        {
            var claim = User.FindFirst("empresaId")?.Value;
            if (int.TryParse(claim, out var id) && id > 0)
                return id;

            var header = Request.Headers["X-Empresa-Id"].ToString();
            if (int.TryParse(header, out id) && id > 0)
                return id;

            if (HttpContext.Items.TryGetValue("EmpresaId", out var v)
                && int.TryParse(v?.ToString(), out id) && id > 0)
                return id;

            return 0;
        }
    }

    [HttpGet]
    public async Task<ActionResult<PaginadoResultadoDto<ProcessoListItemDto>>> Listar(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
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

    [HttpGet("{id:int}/historico")]
    public async Task<ActionResult<IEnumerable<ProcessoHistoricoDto>>> ListarHistorico(int id)
    {
        var emp = EmpresaId; if (emp <= 0) return BadRequest("EmpresaId ausente");
        var historico = await _app.ListarHistoricos(emp, id);
        return Ok(historico);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> AtualizarStatus(int id, [FromBody] ProcessoStatusAtualizarDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Status))
            return BadRequest(new { message = "Informe um status válido." });

        var emp = EmpresaId; if (emp <= 0) return BadRequest("EmpresaId ausente");

        try
        {
            await _app.AtualizarStatus(emp, id, dto.Status, dto.UsuarioId, dto.UsuarioNome);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
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
