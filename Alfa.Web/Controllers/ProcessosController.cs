using Microsoft.AspNetCore.Mvc;

public class ProcessosController : Controller
{
    private readonly ApiClient _api;
    public ProcessosController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index(int page = 1)
    {
        int empresaId = 1; // por enquanto fixo;
        var (total, items) = await _api.GetProcessesAsync(empresaId, page, 10);
        ViewBag.Total = total; ViewBag.Page = page;
        return View(items);
    }

    [HttpGet]
    public IActionResult Novo() => View();

    [HttpPost]
    public async Task<IActionResult> Novo(string titulo)
    {
        int empresaId = 1;
        var resp = await _api.CreateProcessAsync(titulo, empresaId);
        if (!resp.IsSuccessStatusCode) ModelState.AddModelError("", "Erro ao criar processo.");
        return RedirectToAction(nameof(Index));
    }
}
