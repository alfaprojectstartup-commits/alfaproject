using Alfa.Web.Dtos;
using Alfa.Web.Models;
using Alfa.Web.Servicos;
using Alfa.Web.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alfa.Web.Controllers;

[Authorize]
public class TemplatesController : Controller
{
    private const string FasePurpose = "TemplatesController.FaseModeloId";

    private readonly IFaseServico _faseServico;
    private readonly IUrlProtector _urlProtector;

    public TemplatesController(IFaseServico faseServico, IUrlProtector urlProtector)
    {
        _faseServico = faseServico;
        _urlProtector = urlProtector;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var fases = await _faseServico.ObterModelosAsync() ?? new List<FaseModelosViewModel>();
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

                if (fase.Id > 0)
                {
                    fase.Token = EncodeFaseId(fase.Id);
                }

                return fase;
            })
            .ToList();

        return View(ordenadas);
    }

    [HttpGet]
    public async Task<IActionResult> Novo()
    {
        var modelo = new FaseModeloFormViewModel
        {
            Ativo = true,
            Paginas = new List<PaginaModeloFormViewModel>()
        };

        await PopularCatalogoAsync(modelo);
        return View("Form", modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Novo(FaseModeloFormViewModel model)
    {
        PrepararFormulario(model);
        ModelState.Clear();
        TryValidateModel(model);
        ValidarEstrutura(model);

        if (!ModelState.IsValid)
        {
            await PopularCatalogoAsync(model);
            return View("Form", model);
        }

        var payload = MapearParaDto(model);
        var resposta = await _faseServico.CriarModeloAsync(payload);
        if (!resposta.IsSuccessStatusCode)
        {
            var corpo = await resposta.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Não foi possível criar a fase: {(int)resposta.StatusCode} {resposta.ReasonPhrase}. {corpo}");
            await PopularCatalogoAsync(model);
            return View("Form", model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(string token)
    {
        if (!TryDecodeFaseId(token, out var id))
        {
            return NotFound();
        }

        var fase = await _faseServico.ObterModeloPorIdAsync(id);
        if (fase is null) return NotFound();

        var modelo = new FaseModeloFormViewModel
        {
            Id = fase.Id,
            Titulo = fase.Titulo,
            Ordem = fase.Ordem,
            Ativo = fase.Ativo,
            Paginas = (fase.Paginas ?? new List<PaginaModeloViewModel>())
                .Select(p => new PaginaModeloFormViewModel(p))
                .ToList()
        };

        await PopularCatalogoAsync(modelo);
        ViewBag.FaseToken = EncodeFaseId(id);
        return View("Form", modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(string token, FaseModeloFormViewModel model)
    {
        if (!TryDecodeFaseId(token, out var id))
        {
            return NotFound();
        }

        model.Id = id;
        PrepararFormulario(model);
        ModelState.Clear();
        TryValidateModel(model);
        ValidarEstrutura(model);

        if (!ModelState.IsValid)
        {
            await PopularCatalogoAsync(model);
            ViewBag.FaseToken = EncodeFaseId(id);
            return View("Form", model);
        }

        var payload = MapearParaDto(model);
        var resposta = await _faseServico.AtualizarModeloAsync(id, payload);
        if (!resposta.IsSuccessStatusCode)
        {
            var corpo = await resposta.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Não foi possível atualizar a fase: {(int)resposta.StatusCode} {resposta.ReasonPhrase}. {corpo}");
            await PopularCatalogoAsync(model);
            ViewBag.FaseToken = EncodeFaseId(id);
            return View("Form", model);
        }

        return RedirectToAction(nameof(Index));
    }

    private string EncodeFaseId(int id) => _urlProtector.Encode(FasePurpose, id);

    private bool TryDecodeFaseId(string? token, out int id) => _urlProtector.TryDecode(FasePurpose, token, out id);

    private static void PrepararFormulario(FaseModeloFormViewModel model)
    {
        model.Titulo = model.Titulo?.Trim() ?? string.Empty;

        var paginasValidas = new List<PaginaModeloFormViewModel>();
        foreach (var pagina in model.Paginas ?? Enumerable.Empty<PaginaModeloFormViewModel>())
        {
            if (pagina is null) continue;

            pagina.Titulo = pagina.Titulo?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(pagina.Titulo)) continue;

            var camposValidos = new List<CampoModeloFormViewModel>();
            foreach (var campo in pagina.Campos ?? Enumerable.Empty<CampoModeloFormViewModel>())
            {
                if (campo is null) continue;

                campo.NomeCampo = campo.NomeCampo?.Trim() ?? string.Empty;
                campo.Rotulo = campo.Rotulo?.Trim() ?? string.Empty;
                campo.Tipo = campo.Tipo?.Trim() ?? string.Empty;
                campo.Placeholder = string.IsNullOrWhiteSpace(campo.Placeholder) ? null : campo.Placeholder.Trim();
                campo.Mascara = string.IsNullOrWhiteSpace(campo.Mascara) ? null : campo.Mascara.Trim();
                campo.Ajuda = string.IsNullOrWhiteSpace(campo.Ajuda) ? null : campo.Ajuda.Trim();

                if (string.IsNullOrWhiteSpace(campo.NomeCampo)
                    || string.IsNullOrWhiteSpace(campo.Rotulo)
                    || string.IsNullOrWhiteSpace(campo.Tipo))
                {
                    continue;
                }

                var opcoesValidas = new List<CampoOpcaoFormViewModel>();
                foreach (var opcao in campo.Opcoes ?? Enumerable.Empty<CampoOpcaoFormViewModel>())
                {
                    if (opcao is null) continue;

                    opcao.Texto = opcao.Texto?.Trim() ?? string.Empty;
                    opcao.Valor = string.IsNullOrWhiteSpace(opcao.Valor) ? null : opcao.Valor.Trim();

                    if (string.IsNullOrWhiteSpace(opcao.Texto))
                    {
                        continue;
                    }

                    opcoesValidas.Add(opcao);
                }

                campo.Opcoes = opcoesValidas;
                camposValidos.Add(campo);
            }

            pagina.Campos = camposValidos;
            paginasValidas.Add(pagina);
        }

        model.Paginas = paginasValidas;
    }

    private void ValidarEstrutura(FaseModeloFormViewModel model)
    {
        if (!model.Paginas.Any())
        {
            ModelState.AddModelError(string.Empty, "Inclua ao menos uma página na fase.");
            return;
        }

        if (model.Paginas.Any(p => !p.Campos.Any()))
        {
            ModelState.AddModelError(string.Empty, "Cada página deve possuir pelo menos um campo.");
        }
    }

    private static FaseTemplateInputDto MapearParaDto(FaseModeloFormViewModel model)
    {
        var dto = new FaseTemplateInputDto
        {
            Titulo = model.Titulo,
            Ordem = model.Ordem,
            Ativo = model.Ativo,
            Paginas = model.Paginas
                .Select(p => new PaginaTemplateInputDto
                {
                    Titulo = p.Titulo,
                    Ordem = p.Ordem,
                    Campos = p.Campos
                        .Select(c => new CampoTemplateInputDto
                        {
                            NomeCampo = c.NomeCampo,
                            Rotulo = c.Rotulo,
                            Tipo = c.Tipo,
                            Obrigatorio = c.Obrigatorio,
                            Ordem = c.Ordem,
                            Placeholder = c.Placeholder,
                            Mascara = c.Mascara,
                            Ajuda = c.Ajuda,
                            Opcoes = c.Opcoes
                                .Select(o => new CampoOpcaoTemplateInputDto
                                {
                                    Texto = o.Texto,
                                    Valor = o.Valor,
                                    Ordem = o.Ordem,
                                    Ativo = o.Ativo
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList()
        };

        return dto;
    }

    private Task PopularCatalogoAsync(FaseModeloFormViewModel model)
    {
        var catalogoPadrao = new List<CampoModeloViewModel>
        {
            new()
            {
                Rotulo = "Texto",
                NomeCampo = "texto",
                Tipo = "Text"
            },
            new()
            {
                Rotulo = "Data",
                NomeCampo = "data",
                Tipo = "Date"
            },
            new()
            {
                Rotulo = "Dropdown",
                NomeCampo = "dropdown",
                Tipo = "Select"
            },
            new()
            {
                Rotulo = "Opções checkbox",
                NomeCampo = "opcoes_checkbox",
                Tipo = "CheckboxList"
            },
            new()
            {
                Rotulo = "Assinatura",
                NomeCampo = "assinatura",
                Tipo = "Signature"
            },
            new()
            {
                Rotulo = "Imagem",
                NomeCampo = "imagem",
                Tipo = "Image"
            }
        };

        model.CatalogoCampos = catalogoPadrao;
        return Task.CompletedTask;
    }
}
