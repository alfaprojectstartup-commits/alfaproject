using System.Collections.Generic;
using Alfa.Api.Aplicacao.Interfaces;
using Alfa.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RespostasController : ControllerBase
{
    private readonly IProcessoApp _processoApp;

    public RespostasController(IProcessoApp processoApp)
    {
        _processoApp = processoApp;
    }
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
