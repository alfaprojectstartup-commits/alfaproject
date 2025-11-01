using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alfa.Web.Models;
using Alfa.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Alfa.Web.Controllers;

public class TemplatesController : Controller
{
    private readonly ApiClient _apiClient;

    public TemplatesController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var fases = await _apiClient.GetFaseTemplatesAsync() ?? new List<FaseModelosViewModel>();
        var ordenadas = fases
            .OrderBy(f => f.Ordem)
            .ThenBy(f => f.Titulo)
            .Select(fase =>
            {
                fase.Paginas = (fase.Paginas ?? new List<PaginaModeloViewModel>())
                    .OrderBy(p => p.Ordem)
                    .ThenBy(p => p.Titulo)
                    .Select(pagina =>
                    {
                        pagina.Campos = (pagina.Campos ?? new List<CampoModeloViewModel>())
                            .OrderBy(c => c.Ordem)
                            .ThenBy(c => c.Rotulo)
                            .ToList();
                        return pagina;
                    })
                    .ToList();

                return fase;
            })
            .ToList();

        return View(ordenadas);
    }
}
