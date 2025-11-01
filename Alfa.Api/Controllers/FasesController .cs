using System.Linq;
using System.Threading.Tasks;
using Alfa.Api.Repositorios.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
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

    private int EmpresaId =>
        HttpContext.Items.TryGetValue("EmpresaId", out var v) && v is int id ? id : 0;

    // GET api/fases/templates
    [HttpGet("modelos")]
    public async Task<IActionResult> Templates()
    {
        var emp = EmpresaId;
        if (emp <= 0) return BadRequest("EmpresaId ausente.");

        var fases = (await _repo.ListarTemplatesAsync(emp)).ToList();

        foreach (var fase in fases)
        {
            var paginas = (await _paginas.ListarTemplatesPorFaseModeloAsync(emp, fase.Id)).ToList();

            foreach (var pagina in paginas)
            {
                var campos = (await _campos.ListarPorPaginaModeloAsync(emp, pagina.Id)).ToList();

                foreach (var campo in campos)
                {
                    var opcoes = await _campos.ListarOpcoesAsync(emp, campo.Id);
                    campo.Opcoes = opcoes.ToList();
                }

                pagina.Campos = campos;
            }

            fase.Paginas = paginas;
        }

        return Ok(fases);
    }
}
