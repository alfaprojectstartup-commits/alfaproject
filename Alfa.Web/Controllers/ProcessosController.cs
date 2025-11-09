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
using Alfa.Web.Servicos;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

public class ProcessosController : Controller
{
    private const string ProcessoPurpose = "ProcessosController.ProcessoId";

    private readonly ApiClient _api;
    private readonly PreenchimentoExternoTokenService _tokenService;
    private readonly IUrlProtector _urlProtector;
    private readonly IProcessoPdfGenerator _pdfGenerator;

    public ProcessosController(ApiClient api, PreenchimentoExternoTokenService tokenService, IUrlProtector urlProtector, IProcessoPdfGenerator pdfGenerator)
    {
        _api = api;
        _tokenService = tokenService;
        _urlProtector = urlProtector;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<IActionResult> Index(int page = 1, string tab = "ativos")
    {
        var status = tab == "finalizados" ? "Concluído" : null;
        var res = await _api.GetProcessosAsync(page, 10, status)
          ?? new PaginadoResultadoDto<ProcessoListaItemViewModel>
          {
              total = 0,
              items = Enumerable.Empty<ProcessoListaItemViewModel>()
          };

        var itens = res.items.ToList();
        foreach (var processo in itens)
        {
            if (processo.Id > 0)
            {
                processo.Token = EncodeProcessoId(processo.Id);
            }
        }

        ViewBag.Tab = tab;
        ViewBag.Page = page;
        ViewBag.PageSize = 10;
        ViewBag.Total = res.total;
        return View(itens);
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
    public async Task<IActionResult> Detalhes(string token)
    {
        if (!TryDecodeProcessoId(token, out var id))
        {
            return NotFound();
        }

        var processo = await _api.GetProcessoAsync(id);
        if (processo is null) return NotFound();
        OrdenarProcesso(processo);
        return View(processo);
    }

    private string EncodeProcessoId(int id) => _urlProtector.Encode(ProcessoPurpose, id);

    private bool TryDecodeProcessoId(string? token, out int id) => _urlProtector.TryDecode(ProcessoPurpose, token, out id);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkPreenchimentoExterno([FromBody] LinkPreenchimentoExternoRequest? input)
    {
        if (input is null)
        {
            return BadRequest(new { message = "Dados inválidos." });
        }

        if (input.ProcessoId <= 0 || input.FaseInstanciaId <= 0 || input.PaginaInstanciaId <= 0)
        {
            return BadRequest(new { message = "Identificadores inválidos." });
        }

        var processo = await _api.GetProcessoAsync(input.ProcessoId);
        if (processo is null)
        {
            return NotFound(new { message = "Processo não encontrado." });
        }

        var fase = processo.Fases?.FirstOrDefault(f => f.Id == input.FaseInstanciaId);
        var pagina = fase?.Paginas?.FirstOrDefault(p => p.Id == input.PaginaInstanciaId);
        if (fase is null || pagina is null)
        {
            return NotFound(new { message = "Página não encontrada." });
        }

        pagina.Campos = pagina.Campos?.OrderBy(c => c.Ordem).ToList() ?? new List<CampoInstanciaViewModel>();

        TimeSpan? validade = null;
        if (input.ValidadeMinutos.HasValue && input.ValidadeMinutos.Value > 0)
        {
            validade = TimeSpan.FromMinutes(input.ValidadeMinutos.Value);
        }

        var token = _tokenService.GerarToken(processo.Id, fase.Id, pagina.Id, validade);
        var host = Request.Host.HasValue ? Request.Host.Value : null;
        var link = Url.Action(nameof(PreenchimentoExterno), "Processos", new { token }, Request.Scheme, host)
            ?? Url.Action(nameof(PreenchimentoExterno), "Processos", new { token });

        return Json(new { link, token });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var processo = await _api.GetProcessoAsync(id);
        if (processo is null) return NotFound();

        OrdenarProcesso(processo);

        var pdf = _pdfGenerator.Gerar(processo);
        var fileName = $"processo-{processo.Id}.pdf";
        return File(pdf, "application/pdf", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> PreenchimentoExterno(string token)
    {
        if (!_tokenService.TryValidarToken(token, out var payload) || payload is null)
        {
            return BadRequest("Token inválido ou expirado.");
        }

        var processo = await _api.GetProcessoAsync(payload.ProcessoId);
        if (processo is null)
        {
            return NotFound();
        }

        var fase = processo.Fases?.FirstOrDefault(f => f.Id == payload.FaseInstanciaId);
        var pagina = fase?.Paginas?.FirstOrDefault(p => p.Id == payload.PaginaInstanciaId);
        if (fase is null || pagina is null)
        {
            return NotFound();
        }

        pagina.Campos = pagina.Campos?.OrderBy(c => c.Ordem).ToList() ?? new List<CampoInstanciaViewModel>();

        var vm = new PreenchimentoExternoViewModel
        {
            ProcessoId = processo.Id,
            ProcessoTitulo = processo.Titulo ?? string.Empty,
            FaseInstanciaId = fase.Id,
            FaseTitulo = fase.Titulo ?? string.Empty,
            Pagina = pagina,
            Token = token
        };

        ViewBag.HideChrome = true;
        ViewData["Title"] = $"{pagina.Titulo} · Preenchimento externo";

        return View("PreenchimentoExterno", vm);
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

    private static void OrdenarProcesso(ProcessoDetalheViewModel processo)
    {
        processo.Fases ??= new List<FaseInstanciaViewModel>();
        processo.Fases = processo.Fases
            .OrderBy(f => f.Ordem)
            .ToList();

        foreach (var fase in processo.Fases)
        {
            fase.Paginas = fase.Paginas?.OrderBy(p => p.Ordem).ToList() ?? new List<PaginaInstanciaViewModel>();
            foreach (var pagina in fase.Paginas)
            {
                pagina.Campos = pagina.Campos?.OrderBy(c => c.Ordem).ToList() ?? new List<CampoInstanciaViewModel>();
            }
        }
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

public class LinkPreenchimentoExternoRequest
{
    public int ProcessoId { get; set; }
    public int FaseInstanciaId { get; set; }
    public int PaginaInstanciaId { get; set; }
    public int? ValidadeMinutos { get; set; }
}

public class PreenchimentoExternoViewModel
{
    public int ProcessoId { get; set; }
    public string ProcessoTitulo { get; set; } = string.Empty;
    public int FaseInstanciaId { get; set; }
    public string FaseTitulo { get; set; } = string.Empty;
    public PaginaInstanciaViewModel Pagina { get; set; } = new();
    public string Token { get; set; } = string.Empty;
}

