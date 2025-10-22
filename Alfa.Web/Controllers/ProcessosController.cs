using Alfa.Web.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

public class ProcessosController : Controller
{
    private readonly ApiClient _api;
    public ProcessosController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index(int page = 1, string tab = "ativos")
    {
        var status = tab == "finalizados" ? "Concluido" : null;
        var res = await _api.GetProcessosAsync(page, 10, status) ?? new(0, Enumerable.Empty<ProcessoListItemVm>());
        ViewBag.Tab = tab;
        ViewBag.Page = page;
        ViewBag.PageSize = 10;
        ViewBag.Total = res.total;
        return View(res.items);
    }

    [HttpGet]
    public IActionResult Novo() => View(new NovoProcessoVm());

    [HttpPost]
    public async Task<IActionResult> Novo(NovoProcessoVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var fases = vm.FasesSelecionadasIds?.ToArray() ?? Array.Empty<int>();
        var resp = await _api.CriarProcessoAsync(vm.Titulo!, fases);
        if (!resp.IsSuccessStatusCode) { ModelState.AddModelError("", "Falha ao criar processo."); return View(vm); }
        TempData["ok"] = "Processo criado.";
        return RedirectToAction(nameof(Index));
    }
}

public class NovoProcessoVm
{
    [Required] public string? Titulo { get; set; }
    public List<int> FasesSelecionadasIds { get; set; } = new();
}
