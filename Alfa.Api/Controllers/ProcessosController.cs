using Alfa.Api.Aplicacao;
using Alfa.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Alfa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcessosController(IProcessoApp app) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Paginado<ProcessoListaDto>>> Listar(
        [FromQuery] int empresaId, [FromQuery] int pagina = 1, [FromQuery] int tamanho = 10, CancellationToken ct = default)
    {
        if (empresaId <= 0) return BadRequest("empresaId é obrigatório.");
        var res = await app.ListarAsync(empresaId, pagina, tamanho, ct);
        return Ok(res);
    }

    [HttpPost]
    public async Task<ActionResult> Criar([FromBody] ProcessoCriarDto dto, CancellationToken ct = default)
    {
        var id = await app.CriarAsync(dto, ct);
        return CreatedAtAction(nameof(Obter), new { id, empresaId = dto.EmpresaId }, new { id });
    }

    [HttpGet("{id:int}")]
    public ActionResult Obter(int id, [FromQuery] int empresaId)
        => Ok(new { id, empresaId });

    [HttpPut("{id:int}/titulo")]
    public async Task<ActionResult> AtualizarTitulo(int id, [FromQuery] int empresaId, [FromBody] ProcessoAtualizarTituloDto dto, CancellationToken ct = default)
    {
        var ok = await app.AtualizarTituloAsync(id, empresaId, dto, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Excluir(int id, [FromQuery] int empresaId, CancellationToken ct = default)
    {
        var ok = await app.ExcluirAsync(id, empresaId, ct);
        return ok ? NoContent() : NotFound();
    }
}