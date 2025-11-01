using System.Linq;
using System.Threading.Tasks;
using Alfa.Api.Repositorios.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CamposController : ControllerBase
{
    private readonly ICampoRepositorio _campos;

    public CamposController(ICampoRepositorio campos)
    {
        _campos = campos;
    }

    private int EmpresaId =>
        HttpContext.Items.TryGetValue("EmpresaId", out var v) && v is int id ? id : 0;

    [HttpGet("catalogo")]
    public async Task<IActionResult> Catalogo()
    {
        var empresa = EmpresaId;
        if (empresa <= 0) return BadRequest("EmpresaId ausente.");

        var campos = (await _campos.ListarCatalogoAsync(empresa)).ToList();
        return Ok(campos);
    }
}
