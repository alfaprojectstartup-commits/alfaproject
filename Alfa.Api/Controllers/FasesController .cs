using Alfa.Api.Repositorios.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class FasesController : ControllerBase
{
    private readonly IFaseRepositorio _repo;
    public FasesController(IFaseRepositorio repo) => _repo = repo;

    private int EmpresaId =>
        HttpContext.Items.TryGetValue("EmpresaId", out var v) && v is int id ? id : 0;

    // GET api/fases/templates
    [HttpGet("modelos")]
    public async Task<IActionResult> Templates()
    {
        var emp = EmpresaId;
        if (emp <= 0) return BadRequest("EmpresaId ausente.");
        var lista = await _repo.ListarTemplatesAsync(emp);
        return Ok(lista);
    }
}
