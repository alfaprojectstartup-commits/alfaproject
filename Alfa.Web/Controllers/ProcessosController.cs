using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text;
using Alfa.Web.Dtos;
using Alfa.Web.Models;
using Alfa.Web.Services;
using Alfa.Web.Servicos;
using Microsoft.AspNetCore.Http;
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
        OrdenarEstruturaProcesso(processo);
        return View(processo);
    }

    [HttpGet]
    public async Task<IActionResult> Pagina(int processoId, int faseId, int paginaId, int? empresaId = null)
    {
        int? resolvedEmpresaId = null;

        if (empresaId is > 0)
        {
            HttpContext.Session.SetInt32("EmpresaId", empresaId.Value);
            resolvedEmpresaId = empresaId.Value;
        }
        else
        {
            var sessionEmpresaId = HttpContext.Session.GetInt32("EmpresaId");
            if (sessionEmpresaId is > 0)
            {
                resolvedEmpresaId = sessionEmpresaId;
            }
        }

        if (resolvedEmpresaId is > 0)
        {
            ViewData["EmpresaIdOverride"] = resolvedEmpresaId.Value;
        }

        if (resolvedEmpresaId is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var processo = await _api.GetProcessoAsync(processoId);
        if (processo is null) return NotFound();

        OrdenarEstruturaProcesso(processo);

        var fase = processo.Fases?.FirstOrDefault(f => f.Id == faseId);
        if (fase is null) return NotFound();

        var pagina = fase.Paginas?.FirstOrDefault(p => p.Id == paginaId);
        if (pagina is null) return NotFound();

        var vm = new PaginaExternaViewModel
        {
            ProcessoId = processo.Id,
            ProcessoTitulo = processo.Titulo,
            FaseId = fase.Id,
            FaseTitulo = fase.Titulo,
            Pagina = pagina
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ExportarPdf(int id)
    {
        var processo = await _api.GetProcessoAsync(id);
        if (processo is null) return NotFound();

        OrdenarEstruturaProcesso(processo);
        var pdfBytes = ProcessoPdfGenerator.Gerar(processo);
        var nomeArquivo = CriarNomeArquivoPdf(processo.Titulo);

        return File(pdfBytes, "application/pdf", nomeArquivo);
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

    private static void OrdenarEstruturaProcesso(ProcessoDetalheViewModel processo)
    {
        if (processo is null)
        {
            return;
        }

        var fases = processo.Fases ?? new List<FaseInstanciaViewModel>();
        processo.Fases = fases
            .OrderBy(f => f.Ordem)
            .Select(fase =>
            {
                fase.Paginas = (fase.Paginas ?? new List<PaginaInstanciaViewModel>())
                    .OrderBy(p => p.Ordem)
                    .Select(pagina =>
                    {
                        pagina.Campos = (pagina.Campos ?? new List<CampoInstanciaViewModel>())
                            .OrderBy(c => c.Ordem)
                            .ToList();
                        return pagina;
                    })
                    .ToList();
                return fase;
            })
            .ToList();
    }

    private static string CriarNomeArquivoPdf(string titulo)
    {
        var normalizado = string.IsNullOrWhiteSpace(titulo)
            ? "processo"
            : new string(titulo.Trim()
                .Normalize(System.Text.NormalizationForm.FormD)
                .Where(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) || ch == '-')
                .ToArray())
                .Replace(' ', '-');

        if (string.IsNullOrWhiteSpace(normalizado))
        {
            normalizado = "processo";
        }

        return $"{normalizado}-detalhes.pdf";
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

