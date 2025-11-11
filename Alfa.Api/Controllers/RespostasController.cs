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

    [HttpPost("pagina")]
    public async Task<ActionResult> SalvarPagina([FromBody] PaginaRespostaDto dto)
    {
        var processoId = await _processoApp.ObterProcessoIdDaFase(dto.FaseInstanciaId);
        if (!processoId.HasValue)
        {
            return NotFound("Fase não encontrada.");
        }

        try
        {
            await _processoApp.RegistrarResposta(processoId.Value, dto);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
