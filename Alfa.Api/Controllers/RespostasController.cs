using Alfa.Api.Aplicacao.Interfaces;
using Alfa.Api.Dados;
using Alfa.Api.Dtos;
using Alfa.Api.Repositorios.Interfaces;
using Dapper;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RespostasController : ControllerBase
{
    private readonly IRespostaRepositorio _resp;
    private readonly IFaseRepositorio _fase;
    private readonly IProcessoApp _proc;
    public RespostasController(IRespostaRepositorio r, IFaseRepositorio f, IProcessoApp p) { _resp = r; _fase = f; _proc = p; }
    private int EmpresaId =>
    HttpContext.Items.TryGetValue("EmpresaId", out var v)
    && int.TryParse(v?.ToString(), out var id)
        ? id
        : 0;

    [HttpPost("pagina")]
    public async Task<ActionResult> SalvarPagina([FromBody] PaginaRespostaDto dto)
    {
        var emp = EmpresaId; if (emp <= 0) return BadRequest("EmpresaId ausente");
        await _resp.SalvarPaginaAsync(emp, dto);

        // Recalcula % da fase
        await _fase.RecalcularProgressoFaseAsync(emp, dto.FasesId);

        // Recalcula % do processo (média)
        // descobrir processoId a partir da fase:
        // (para simplificar, pegue via query rápida)
        var processoId = await ObterProcessoIdDaFase(emp, dto.FasesId);
        await _proc.RecalcularProgressoProcesso(emp, processoId);

        return Ok();
    }

    private async Task<int> ObterProcessoIdDaFase(int emp, int FasesId)
    {
        using var cn = await (HttpContext.RequestServices.GetRequiredService<IConexaoSql>()).AbrirAsync();
        return await cn.ExecuteScalarAsync<int>(
            "SELECT ProcessInstanceId FROM Fases WHERE EmpresaId=@emp AND Id=@id",
            new { emp, id = FasesId });
    }
}
