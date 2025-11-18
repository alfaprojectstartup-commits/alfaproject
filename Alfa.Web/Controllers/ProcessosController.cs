using System.Text.Json.Serialization;
using System.Text.Json;
using System.Net.Http;
using Alfa.Web.Dtos;
using Alfa.Web.Models;
using Alfa.Web.Servicos;
using Alfa.Web.Servicos.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class ProcessosController : Controller
{
    private const string ProcessoPurpose = "ProcessosController.ProcessoId";

    private readonly IProcessoServico _processoServico;
    private readonly IFaseServico _faseServico;
    private readonly PreenchimentoExternoTokenService _tokenService;
    private readonly IUrlProtector _urlProtector;
    private readonly IProcessoPdfGenerator _pdfGenerator;

    public ProcessosController(
        IProcessoServico processoServico,
        IFaseServico faseServico,
        PreenchimentoExternoTokenService tokenService,
        IUrlProtector urlProtector,
        IProcessoPdfGenerator pdfGenerator)
    {
        _processoServico = processoServico;
        _faseServico = faseServico;
        _tokenService = tokenService;
        _urlProtector = urlProtector;
        _pdfGenerator = pdfGenerator;
    }
    public async Task<IActionResult> Index(int page = 1, string tab = "ativos")
    {
        try
        {
            var status = tab == "finalizados" ? "Concluído" : null;
            var res = await _processoServico.ObterProcessosAsync(page, 10, status)
                     ?? new PaginadoResultadoDto<ProcessoListaItemViewModel> { total = 0, items = Enumerable.Empty<ProcessoListaItemViewModel>() };

            var itens = res.items.ToList();
            foreach (var p in itens)
                if (p.Id > 0) p.Token = EncodeProcessoId(p.Id);

            ViewBag.Tab = tab;
            ViewBag.Page = page;
            ViewBag.PageSize = 10;
            ViewBag.Total = res.total;
            return View(itens);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex); 

            return Problem(
                title: "Falha ao carregar processos",
                detail: ex.Message, 
                statusCode: 500);
        }
    }


    [HttpGet]
    public async Task<IActionResult> Novo()
    {
        var fases = await _faseServico.ObterModelosAsync() ?? new List<FaseModelosViewModel>();
        var fasesAtivas = fases
            .Where(f => f.Ativo)
            .OrderBy(f => f.Ordem)
            .ToList();

        var processosPadrao = await _processoServico.ObterProcessoPadraoModelosAsync() ?? new List<ProcessoPadraoModeloViewModel>();
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
        var fasesDisponiveis = (await _faseServico.ObterModelosAsync() ?? new List<FaseModelosViewModel>())
            .Where(f => f.Ativo)
            .OrderBy(f => f.Ordem)
            .ToList();

        vm.FasesDisponiveis = fasesDisponiveis;
        var processosPadrao = await _processoServico.ObterProcessoPadraoModelosAsync() ?? new List<ProcessoPadraoModeloViewModel>();
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

        var resp = await _processoServico.CriarProcessoAsync(titulo, fases);
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
            resp = await _processoServico.CriarProcessoPadraoAsync(input);
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

        var processo = await _processoServico.ObterProcessoAsync(id);
        if (processo is null) return NotFound();
        OrdenarProcesso(processo);
        ViewBag.HistoricoUrl = Url.Action(nameof(Historico), new { token });
        return View(processo);
    }

    [HttpGet]
    public async Task<IActionResult> Dados(int id)
    {
        var processo = await _processoServico.ObterProcessoAsync(id);
        if (processo is null)
        {
            return NotFound();
        }

        OrdenarProcesso(processo);
        return Json(processo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarRespostas([FromBody] RegistrarRespostasInput? input)
    {
        if (input is null || input.ProcessoId <= 0 || input.FaseInstanciaId <= 0 || input.PaginaInstanciaId <= 0)
        {
            return BadRequest(new { message = "Dados inválidos." });
        }

        var payload = input.ToPaginaRespostaInput();

        HttpResponseMessage resposta;
        try
        {
            resposta = await _processoServico.RegistrarRespostasAsync(input.ProcessoId, payload);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { message = "Falha ao chamar a API.", detail = ex.Message });
        }

        if (!resposta.IsSuccessStatusCode)
        {
            var corpo = await resposta.Content.ReadAsStringAsync();
            var mensagem = ExtrairMensagemApi(corpo, "Não foi possível salvar as respostas.");
            return StatusCode((int)resposta.StatusCode, new { message = mensagem });
        }

        return Ok(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarRespostasExterno([FromBody] RegistrarRespostasExternoInput? input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Token))
        {
            return BadRequest(new { message = "Dados inválidos." });
        }

        if (!_tokenService.TryValidarToken(input.Token, out var tokenPayload) || tokenPayload is null)
        {
            return BadRequest(new { message = "Token inválido ou expirado." });
        }

        if (tokenPayload.ProcessoId <= 0 || tokenPayload.FaseInstanciaId <= 0 || tokenPayload.PaginaInstanciaId <= 0)
        {
            return BadRequest(new { message = "Token inválido." });
        }

        //if (input.ProcessoId > 0 && input.ProcessoId != tokenPayload.ProcessoId)
        //{
        //    return BadRequest(new { message = "Token não corresponde ao processo informado." });
        //}

        //if (input.FaseInstanciaId > 0 && input.FaseInstanciaId != tokenPayload.FaseInstanciaId)
        //{
        //    return BadRequest(new { message = "Token não corresponde à fase informada." });
        //}

        //if (input.PaginaInstanciaId > 0 && input.PaginaInstanciaId != tokenPayload.PaginaInstanciaId)
        //{
        //    return BadRequest(new { message = "Token não corresponde à página informada." });
        //}

        //var payload = input.ToPaginaRespostaInput();
        //payload.FaseInstanciaId = tokenPayload.FaseInstanciaId;
        //payload.PaginaInstanciaId = tokenPayload.PaginaInstanciaId;

        HttpResponseMessage resposta;
        try
        {
            //resposta = await _processoServico.RegistrarRespostasAsync(tokenPayload.ProcessoId, payload, input.Token);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { message = "Falha ao chamar a API.", detail = ex.Message });
        }

        //if (!resposta.IsSuccessStatusCode)
        //{
        //    var corpo = await resposta.Content.ReadAsStringAsync();
        //    var mensagem = ExtrairMensagemApi(corpo, "Não foi possível enviar as respostas.");
        //    return StatusCode((int)resposta.StatusCode, new { message = mensagem });
        //}

        return Ok(new { ok = true });
    }

    [HttpGet]
    public async Task<IActionResult> Historico(string token)
    {
        if (!TryDecodeProcessoId(token, out var id))
        {
            return NotFound();
        }

        var processo = await _processoServico.ObterProcessoAsync(id);
        if (processo is null) return NotFound();
        OrdenarProcesso(processo);

        List<ProcessoHistoricoViewModel> historico;
        try
        {
            historico = await _processoServico.ObterHistoricoAsync(id) ?? new List<ProcessoHistoricoViewModel>();
        }
        catch (HttpRequestException)
        {
            historico = new List<ProcessoHistoricoViewModel>();
        }

        var vm = new ProcessoHistoricoPaginaViewModel
        {
            Processo = processo,
            Historico = historico
                .OrderByDescending(h => h.CriadoEm)
                .ToList(),
            Token = token
        };

        return View("Historico", vm);
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

        var processo = await _processoServico.ObterProcessoAsync(input.ProcessoId);
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
        var processo = await _processoServico.ObterProcessoAsync(id);
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

        var processo = await _processoServico.ObterProcessoAsync(payload.ProcessoId);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarStatus([FromBody] ProcessoStatusAlterarInput? input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Token) || string.IsNullOrWhiteSpace(input.Status))
        {
            return BadRequest(new { message = "Dados inválidos." });
        }

        if (!TryDecodeProcessoId(input.Token, out var processoId))
        {
            return NotFound(new { message = "Processo não encontrado." });
        }

        var status = input.Status.Trim();
        if (string.IsNullOrWhiteSpace(status))
        {
            return BadRequest(new { message = "Informe um status válido." });
        }

        var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
        var usuarioNome = HttpContext.Session.GetString("Nome");

        var payload = new ProcessoStatusAtualizarInput
        {
            Status = status,
            UsuarioId = usuarioId,
            UsuarioNome = string.IsNullOrWhiteSpace(usuarioNome) ? null : usuarioNome
        };

        HttpResponseMessage resposta;
        try
        {
            resposta = await _processoServico.AtualizarStatusAsync(processoId, payload);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(500, new { message = "Não foi possível atualizar o status.", detail = ex.Message });
        }

        if (!resposta.IsSuccessStatusCode)
        {
            var corpo = await resposta.Content.ReadAsStringAsync();
            var mensagem = "Não foi possível atualizar o status.";
            if (!string.IsNullOrWhiteSpace(corpo))
            {
                try
                {
                    using var doc = JsonDocument.Parse(corpo);
                    var root = doc.RootElement;
                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
                    {
                        mensagem = msgEl.GetString() ?? mensagem;
                    }
                    else if (root.ValueKind == JsonValueKind.String)
                    {
                        mensagem = root.GetString() ?? mensagem;
                    }
                    else
                    {
                        mensagem = corpo.Trim();
                    }
                }
                catch
                {
                    mensagem = corpo.Trim();
                }
            }

            return StatusCode((int)resposta.StatusCode, new { message = mensagem });
        }

        return Ok(new { status });
    }

    private static string ExtrairMensagemApi(string? corpo, string mensagemPadrao)
    {
        if (string.IsNullOrWhiteSpace(corpo))
        {
            return mensagemPadrao;
        }

        try
        {
            using var doc = JsonDocument.Parse(corpo);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
            {
                var valor = msg.GetString();
                return string.IsNullOrWhiteSpace(valor) ? mensagemPadrao : valor!;
            }

            if (root.ValueKind == JsonValueKind.String)
            {
                var valor = root.GetString();
                return string.IsNullOrWhiteSpace(valor) ? mensagemPadrao : valor!;
            }
        }
        catch
        {
            // ignora e usa corpo puro
        }

        return string.IsNullOrWhiteSpace(corpo) ? mensagemPadrao : corpo.Trim();
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

public class ProcessoStatusAlterarInput
{
    public string Token { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class RegistrarRespostasInput
{
    public int ProcessoId { get; set; }
    public int FaseInstanciaId { get; set; }
    public int PaginaInstanciaId { get; set; }
    public IEnumerable<CampoRespostaInput> Campos { get; set; } = Enumerable.Empty<CampoRespostaInput>();

    public PaginaRespostaInput ToPaginaRespostaInput() => new()
    {
        FaseInstanciaId = FaseInstanciaId,
        PaginaInstanciaId = PaginaInstanciaId,
        Campos = Campos?.Select(c => c.ToFieldResponseInput()).ToList() ?? new List<FieldResponseInput>()
    };
}

public sealed class RegistrarRespostasExternoInput : RegistrarRespostasInput
{
    public string Token { get; set; } = string.Empty;
}

public sealed class CampoRespostaInput
{
    public int CampoInstanciaId { get; set; }
    public string? ValorTexto { get; set; }
    public decimal? ValorNumero { get; set; }
    public DateTime? ValorData { get; set; }
    public bool? ValorBool { get; set; }

    public FieldResponseInput ToFieldResponseInput() => new()
    {
        CampoInstanciaId = CampoInstanciaId,
        ValorTexto = ValorTexto,
        ValorNumero = ValorNumero,
        ValorData = ValorData,
        ValorBool = ValorBool
    };
}

