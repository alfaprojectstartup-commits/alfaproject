using System;
using System.Collections.Generic;
using System.Linq;
using Alfa.Web.Dtos;
using Alfa.Web.Models;
using Alfa.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

public class ProcessosController : Controller
{
    private readonly ApiClient _api;
    public ProcessosController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index(int page = 1, string tab = "ativos")
    {
        var status = tab == "finalizados" ? "Concluído" : null;
        var res = await _api.GetProcessosAsync(page, 10, status)
          ?? new PaginadoResultadoDto<ProcessoListaItemViewModel>
          {
              total = 0,
              items = Enumerable.Empty<ProcessoListaItemViewModel>()
          };

        ViewBag.Tab = tab;
        ViewBag.Page = page;
        ViewBag.PageSize = 10;
        ViewBag.Total = res.total;
        return View(res.items.ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Novo()
    {
        var fases = await _api.GetFaseTemplatesAsync() ?? new List<FaseModelosViewModel>();
        var vm = new NovoProcessoVm
        {
            FasesDisponiveis = fases
                .Where(f => f.Ativo)
                .OrderBy(f => f.Ordem)
                .ToList()
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Novo(NovoProcessoVm vm)
    {
        if (!ModelState.IsValid)
        {
            vm.FasesDisponiveis = (await _api.GetFaseTemplatesAsync() ?? new List<FaseModelosViewModel>())
                .Where(f => f.Ativo)
                .OrderBy(f => f.Ordem)
                .ToList();
            return View(vm);
        }

        var titulo = vm.Titulo!.Trim();
        if (string.IsNullOrWhiteSpace(titulo))
        {
            ModelState.AddModelError(nameof(vm.Titulo), "Informe um título para o processo.");
            vm.FasesDisponiveis = (await _api.GetFaseTemplatesAsync() ?? new List<FaseModelosViewModel>())
                .Where(f => f.Ativo)
                .OrderBy(f => f.Ordem)
                .ToList();
            return View(vm);
        }

        var fases = vm.FasesSelecionadasIds?.ToArray() ?? Array.Empty<int>();
        var resp = await _api.CriarProcessoAsync(titulo, fases);
        if (!resp.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Falha ao criar processo.");
            vm.FasesDisponiveis = (await _api.GetFaseTemplatesAsync() ?? new List<FaseModelosViewModel>())
                .Where(f => f.Ativo)
                .OrderBy(f => f.Ordem)
                .ToList();
            return View(vm);
        }
        TempData["ok"] = "Processo criado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Detalhes(int id)
    {
        var processo = await _api.GetProcessoAsync(id);
        if (processo is null) return NotFound();
        if (processo.Fases is null) processo.Fases = new List<FaseInstanciaViewModel>();
        processo.Fases = processo.Fases.OrderBy(f => f.Ordem).ToList();
        foreach (var fase in processo.Fases)
        {
            fase.Paginas = fase.Paginas?.OrderBy(p => p.Ordem).ToList() ?? new List<PaginaInstanciaViewModel>();
            foreach (var pagina in fase.Paginas)
            {
                pagina.Campos = pagina.Campos?.OrderBy(c => c.Ordem).ToList() ?? new List<CampoInstanciaViewModel>();
            }
        }
        return View(processo);
    }
}

public class NovoProcessoVm
{
    [Required]
    [Display(Name = "Título do processo")]
    public string? Titulo { get; set; }

    public List<int> FasesSelecionadasIds { get; set; } = new();

    public List<FaseModelosViewModel> FasesDisponiveis { get; set; } = new();
}
