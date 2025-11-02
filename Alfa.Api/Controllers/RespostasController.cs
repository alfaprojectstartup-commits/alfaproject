using System.Collections.Generic;
using Alfa.Api.Aplicacao.Interfaces;
using Alfa.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RespostasController : ControllerBase
{
    private readonly IProcessoApp _processoApp;

    public RespostasController(IProcessoApp processoApp)
    {
        _processoApp = processoApp;
    }

    private int EmpresaId =>
        HttpContext.Items.TryGetValue("EmpresaId", out var v)
        && int.TryParse(v?.ToString(), out var id)
            ? id
            : 0;

    [HttpPost("pagina")]
    public async Task<ActionResult> SalvarPagina([FromBody] PaginaRespostaDto dto)
    {
        var emp = EmpresaId; if (emp <= 0) return BadRequest("EmpresaId ausente");

        var processoId = await _processoApp.ObterProcessoIdDaFase(emp, dto.FaseInstanciaId);
        if (!processoId.HasValue)
        {
            return NotFound("Fase não encontrada para a empresa informada.");
        }

        try
        {
            await _processoApp.RegistrarResposta(emp, processoId.Value, dto);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
