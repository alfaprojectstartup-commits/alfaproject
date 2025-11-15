using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alfa.Api.Dtos;
using Alfa.Api.Repositorios.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class FasesController : ControllerBase
{
    private readonly IFaseRepositorio _repo;
    private readonly IPaginaRepositorio _paginas;
    private readonly ICampoRepositorio _campos;

    public FasesController(
        IFaseRepositorio repo,
        IPaginaRepositorio paginas,
        ICampoRepositorio campos)
    {
        _repo = repo;
        _paginas = paginas;
        _campos = campos;
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

    // GET api/fases/templates
    [HttpGet("modelos")]
    public async Task<IActionResult> Templates()
    {
        var emp = EmpresaId;
        if (emp <= 0) return BadRequest("EmpresaId ausente.");

        var fases = (await _repo.ListarTemplatesAsync(emp)).ToList();
        await PreencherEstruturaAsync(emp, fases);
        return Ok(fases);
    }

    // GET api/fases/modelos/{id}
    [HttpGet("modelos/{id:int}")]
    public async Task<IActionResult> Template(int id)
    {
        var emp = EmpresaId;
        if (emp <= 0) return BadRequest("EmpresaId ausente.");

        var fase = await _repo.ObterTemplateAsync(emp, id);
        if (fase is null) return NotFound();

        await PreencherEstruturaAsync(emp, new[] { fase });
        return Ok(fase);
    }

    // POST api/fases/modelos
    [HttpPost("modelos")]
    public async Task<IActionResult> CriarTemplate([FromBody] FaseModeloInputDto dto)
    {
        var emp = EmpresaId;
        if (emp <= 0) return BadRequest("EmpresaId ausente.");

        var faseId = await _repo.CriarTemplateAsync(emp, dto);
        var fase = await _repo.ObterTemplateAsync(emp, faseId);
        if (fase is null) return StatusCode(500, "Falha ao carregar template recém criado.");

        await PreencherEstruturaAsync(emp, new[] { fase });
        return CreatedAtAction(nameof(Template), new { id = fase.Id }, fase);
    }

    // PUT api/fases/modelos/{id}
    [HttpPut("modelos/{id:int}")]
    public async Task<IActionResult> AtualizarTemplate(int id, [FromBody] FaseModeloInputDto dto)
    {
        var emp = EmpresaId;
        if (emp <= 0) return BadRequest("EmpresaId ausente.");

        try
        {
            await _repo.AtualizarTemplateAsync(emp, id, dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        var fase = await _repo.ObterTemplateAsync(emp, id);
        if (fase is null) return NotFound();

        await PreencherEstruturaAsync(emp, new[] { fase });
        return Ok(fase);
    }

    private async Task PreencherEstruturaAsync(int empresaId, IEnumerable<FaseModeloDto> fases)
    {
        foreach (var fase in fases)
        {
            var paginas = (await _paginas.ListarTemplatesPorFaseModeloAsync(empresaId, fase.Id)).ToList();

            foreach (var pagina in paginas)
            {
                var campos = (await _campos.ListarPorPaginaModeloAsync(empresaId, pagina.Id)).ToList();

                foreach (var campo in campos)
                {
                    var opcoes = await _campos.ListarOpcoesAsync(empresaId, campo.Id);
                    campo.Opcoes = opcoes.ToList();
                }

                pagina.Campos = campos;
            }

            fase.Paginas = paginas;
        }
    }
}
