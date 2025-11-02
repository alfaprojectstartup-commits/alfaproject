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
        var fasesAtivas = fases
            .Where(f => f.Ativo)
            .OrderBy(f => f.Ordem)
            .ToList();

        var vm = new NovoProcessoVm
        {
            FasesDisponiveis = fasesAtivas,
            ProcessosPadrao = ConstruirProcessosPadrao(fasesAtivas)
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Novo(NovoProcessoVm vm)
    {
        var fasesDisponiveis = (await _api.GetFaseTemplatesAsync() ?? new List<FaseModelosViewModel>())
            .Where(f => f.Ativo)
            .OrderBy(f => f.Ordem)
            .ToList();

        vm.FasesDisponiveis = fasesDisponiveis;
        vm.ProcessosPadrao = ConstruirProcessosPadrao(fasesDisponiveis);

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var titulo = vm.Titulo!.Trim();
        if (string.IsNullOrWhiteSpace(titulo))
        {
            ModelState.AddModelError(nameof(vm.Titulo), "Informe um título para o processo.");
            return View(vm);
        }

        if (vm.ProcessoPadraoSelecionadoId.HasValue)
        {
            var selecionado = vm.ProcessosPadrao
                .FirstOrDefault(p => p.Id == vm.ProcessoPadraoSelecionadoId.Value);
            if (selecionado is not null)
            {
                vm.FasesSelecionadasIds = selecionado.FaseModeloIds.ToList();
            }
        }

        var fases = vm.FasesSelecionadasIds?.ToArray() ?? Array.Empty<int>();
        if (!fases.Any())
        {
            ModelState.AddModelError(string.Empty, "Selecione um processo padrão ou escolha ao menos uma fase.");
            return View(vm);
        }

        var resp = await _api.CriarProcessoAsync(titulo, fases);
        if (!resp.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Falha ao criar processo.");
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

    private static List<ProcessoPadraoViewModel> ConstruirProcessosPadrao(List<FaseModelosViewModel> fases)
    {
        var padroes = new List<ProcessoPadraoViewModel>();
        if (fases is null || fases.Count == 0)
        {
            return padroes;
        }

        var completo = new ProcessoPadraoViewModel
        {
            Id = 1,
            Titulo = "Processo completo",
            Descricao = "Inclui todas as fases disponíveis nesta empresa.",
            FaseModeloIds = fases.Select(f => f.Id).ToList()
        };
        padroes.Add(completo);

        var fasesIniciais = fases.Take(Math.Min(3, fases.Count)).ToList();
        if (fasesIniciais.Count > 0 && fasesIniciais.Count < fases.Count)
        {
            padroes.Add(new ProcessoPadraoViewModel
            {
                Id = 2,
                Titulo = "Onboarding rápido",
                Descricao = "Seleção das primeiras fases para processos ágeis.",
                FaseModeloIds = fasesIniciais.Select(f => f.Id).ToList()
            });
        }

        return padroes;
    }
}

public class NovoProcessoVm
{
    [Required]
    [Display(Name = "Título do processo")]
    public string? Titulo { get; set; }

    public List<int> FasesSelecionadasIds { get; set; } = new();

    public List<FaseModelosViewModel> FasesDisponiveis { get; set; } = new();

    public List<ProcessoPadraoViewModel> ProcessosPadrao { get; set; } = new();

    public int? ProcessoPadraoSelecionadoId { get; set; }
}

public class ProcessoPadraoViewModel
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public IReadOnlyList<int> FaseModeloIds { get; set; } = Array.Empty<int>();
}

