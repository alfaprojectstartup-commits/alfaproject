using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
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

        var processosPadrao = await _api.GetProcessoPadraoModelosAsync() ?? new List<ProcessoPadraoModeloViewModel>();
        var padroesValidos = FiltrarPadroesValidos(processosPadrao, fasesAtivas);

        var vm = new NovoProcessoVm
        {
            FasesDisponiveis = fasesAtivas,
            ProcessosPadrao = padroesValidos
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
        var processosPadrao = await _api.GetProcessoPadraoModelosAsync() ?? new List<ProcessoPadraoModeloViewModel>();
        var padroesValidos = FiltrarPadroesValidos(processosPadrao, fasesDisponiveis);
        vm.ProcessosPadrao = padroesValidos;

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
            var selecionado = padroesValidos
                .FirstOrDefault(p => p.Id == vm.ProcessoPadraoSelecionadoId.Value);
            if (selecionado is not null)
            {
                var faseDisponivelIds = new HashSet<int>(fasesDisponiveis.Select(f => f.Id));
                vm.FasesSelecionadasIds = selecionado.FaseModeloIds
                    .Where(faseDisponivelIds.Contains)
                    .Distinct()
                    .ToList();
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarPadrao([FromBody] ProcessoPadraoModeloInput payload)
    {
        if (payload is null)
        {
            return BadRequest(new { message = "Dados inválidos." });
        }

        var titulo = payload.Titulo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(titulo))
        {
            return BadRequest(new { message = "Informe um título para o padrão." });
        }

        var descricao = string.IsNullOrWhiteSpace(payload.Descricao) ? null : payload.Descricao!.Trim();
        var fases = payload.FaseModeloIds?
            .Where(id => id > 0)
            .Distinct()
            .ToList() ?? new List<int>();

        if (fases.Count == 0)
        {
            return BadRequest(new { message = "Selecione ao menos uma fase." });
        }

        var input = new ProcessoPadraoModeloInput
        {
            Titulo = titulo,
            Descricao = descricao,
            FaseModeloIds = fases
        };

        HttpResponseMessage? resp;
        try
        {
            resp = await _api.CriarProcessoPadraoAsync(input);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(500, new { message = "Não foi possível salvar o padrão.", detail = ex.Message });
        }

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            return StatusCode((int)resp.StatusCode, new { message = "Falha ao salvar padrão.", detail = body });
        }

        var criado = await resp.Content.ReadFromJsonAsync<CreatedResourceDto>() ?? new CreatedResourceDto();

        return Ok(new
        {
            id = criado.Id,
            titulo = input.Titulo,
            descricao = input.Descricao,
            faseModeloIds = input.FaseModeloIds
        });
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

    private static List<ProcessoPadraoModeloViewModel> FiltrarPadroesValidos(
        IEnumerable<ProcessoPadraoModeloViewModel> padroes,
        IEnumerable<FaseModelosViewModel> fasesDisponiveis)
    {
        if (padroes is null) return new List<ProcessoPadraoModeloViewModel>();

        var faseIds = new HashSet<int>(fasesDisponiveis?.Select(f => f.Id) ?? Enumerable.Empty<int>());
        if (faseIds.Count == 0)
        {
            return new List<ProcessoPadraoModeloViewModel>();
        }

        return padroes
            .Select(p => new ProcessoPadraoModeloViewModel
            {
                Id = p.Id,
                Titulo = p.Titulo,
                Descricao = p.Descricao,
                FaseModeloIds = p.FaseModeloIds?
                    .Where(faseIds.Contains)
                    .Distinct()
                    .ToList() ?? new List<int>()
            })
            .Where(p => p.FaseModeloIds.Count > 0)
            .OrderBy(p => p.Titulo, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

}

public class NovoProcessoVm
{
    [Required]
    [Display(Name = "Título do processo")]
    public string? Titulo { get; set; }

    public List<int> FasesSelecionadasIds { get; set; } = new();

    public List<FaseModelosViewModel> FasesDisponiveis { get; set; } = new();

    public List<ProcessoPadraoModeloViewModel> ProcessosPadrao { get; set; } = new();

    public int? ProcessoPadraoSelecionadoId { get; set; }
}

public class CreatedResourceDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}

