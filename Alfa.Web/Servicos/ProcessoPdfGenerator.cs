using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Alfa.Web.Dtos;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Alfa.Web.Servicos;

public interface IProcessoPdfGenerator
{
    byte[] Gerar(ProcessoDetalheViewModel processo);
}

public class ProcessoPdfGenerator : IProcessoPdfGenerator
{
    private static bool _licenseConfigured;
    private static readonly ImageSource PlaceholderLogo = ImageSource.FromBinary(() => Placeholders.Image(160, 60));

    public ProcessoPdfGenerator()
    {
        if (!_licenseConfigured)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            _licenseConfigured = true;
        }
    }

    public byte[] Gerar(ProcessoDetalheViewModel processo)
    {
        ArgumentNullException.ThrowIfNull(processo);

        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        var fases = (processo.Fases ?? new List<FaseInstanciaViewModel>())
            .OrderBy(f => f.Ordem)
            .ToList();

        foreach (var fase in fases)
        {
            fase.Paginas = (fase.Paginas ?? new List<PaginaInstanciaViewModel>())
                .OrderBy(p => p.Ordem)
                .ToList();

            foreach (var pagina in fase.Paginas)
            {
                pagina.Campos = (pagina.Campos ?? new List<CampoInstanciaViewModel>())
                    .OrderBy(c => c.Ordem)
                    .ToList();
            }
        }

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Grey.Darken3));
                page.PageColor(Colors.Grey.Lighten5);

                page.Header().Element(header => MontarCabecalho(header, processo, cultura));

                page.Content().PaddingVertical(15).Column(column =>
                {
                    column.Spacing(15);

                    column.Item().Element(resumo => MontarResumoProcesso(resumo, processo, fases, cultura));

                    column.Item().Text("Detalhamento das fases")
                        .FontSize(14)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken3);

                    if (!fases.Any())
                    {
                        column.Item().Border(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Background(Colors.White)
                            .Padding(20)
                            .Text("Nenhuma fase disponível para este processo.");
                        return;
                    }

                    foreach (var fase in fases)
                    {
                        column.Item().Element(item => MontarFase(item, fase, cultura));
                    }
                });

                page.Footer().Element(footer => MontarRodape(footer, cultura));
            });
        });

        return documento.GeneratePdf();
    }

    private static void MontarCabecalho(IContainer container, ProcessoDetalheViewModel processo, CultureInfo cultura)
    {
        container.PaddingBottom(10)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Row(row =>
            {
                row.ConstantItem(90)
                    .Height(60)
                    .AlignMiddle()
                    .Image(PlaceholderLogo);

                row.RelativeItem().Column(column =>
                {
                    column.Spacing(2);
                    column.Item().Text("Relatório do Processo")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);
                    column.Item().Text(processo.Titulo)
                        .FontSize(20)
                        .SemiBold();
                });

                row.ConstantItem(160).Column(column =>
                {
                    column.Spacing(2);
                    column.Item().Text(text =>
                    {
                        text.Span("Status · ").SemiBold();
                        text.Span(processo.Status);
                    });
                    column.Item().Text(text =>
                    {
                        text.Span("Criado em · ").SemiBold();
                        text.Span(processo.CriadoEm.ToString("dd/MM/yyyy HH:mm", cultura));
                    });
                });
            });
    }

    private static void MontarResumoProcesso(IContainer container, ProcessoDetalheViewModel processo, IReadOnlyCollection<FaseInstanciaViewModel> fases, CultureInfo cultura)
    {
        var totalPaginas = fases.Sum(f => f.Paginas.Count);
        var totalCampos = fases.Sum(f => f.Paginas.Sum(p => p.Campos.Count));
        var fasesConcluidas = fases.Count(f => f.PorcentagemProgresso >= 100);

        container.Background(Colors.White)
            .Padding(20)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten3)
            .Column(column =>
            {
                column.Spacing(12);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(info =>
                    {
                        info.Spacing(4);
                        info.Item().Text("Resumo geral")
                            .FontSize(12)
                            .SemiBold();
                        info.Item().Text(text =>
                        {
                            text.Span("Andamento: ").SemiBold();
                            text.Span($"{processo.PorcentagemProgresso}%");
                        });
                        info.Item().Text(text =>
                        {
                            text.Span("Identificador: ").SemiBold();
                            text.Span($"#{processo.Id}");
                        });
                    });

                    row.RelativeItem().PaddingLeft(10).Grid(grid =>
                    {
                        grid.Columns(3);
                        grid.Item().Element(item => MontarIndicador(item, "Fases", fases.Count.ToString()));
                        grid.Item().Element(item => MontarIndicador(item, "Páginas", totalPaginas.ToString()));
                        grid.Item().Element(item => MontarIndicador(item, "Campos", totalCampos.ToString()));
                    });
                });

                if (fases.Any())
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(item =>
                        {
                            var valor = fasesConcluidas == fases.Count
                                ? "Todas as fases concluídas"
                                : $"{fasesConcluidas} de {fases.Count} fases concluídas";

                            item.Background(Colors.Blue.Lighten5)
                                .Padding(10)
                                .Border(1)
                                .BorderColor(Colors.Blue.Lighten4)
                                .Text(valor)
                                .FontColor(Colors.Blue.Darken2);
                        });
                    });
                }
            });
    }

    private static void MontarIndicador(IContainer container, string titulo, string valor)
    {
        container.Column(column =>
        {
            column.Spacing(2);
            column.Item().Text(titulo)
                .FontSize(9)
                .FontColor(Colors.Grey.Darken1);
            column.Item().Text(valor)
                .FontSize(16)
                .SemiBold();
        });
    }

    private static void MontarRodape(IContainer container, CultureInfo cultura)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Página ");
            text.CurrentPageNumber();
            text.Span(" de ");
            text.TotalPages();
            text.Span(" · Gerado em ");
            text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm", cultura));
        });
    }

    private static void MontarFase(IContainer container, FaseInstanciaViewModel fase, CultureInfo cultura)
    {
        container.Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.White)
            .Padding(18)
            .Column(column =>
            {
                column.Spacing(8);
                column.Item().Text(fase.Titulo)
                    .FontSize(16)
                    .SemiBold();
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(fase.Status)
                        .FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(120).AlignRight().Text($"{fase.PorcentagemProgresso}%")
                        .FontSize(12)
                        .SemiBold();
                });

                if (!fase.Paginas.Any())
                {
                    column.Item().Text("Nenhuma página registrada nesta fase.");
                    return;
                }

                foreach (var pagina in fase.Paginas)
                {
                    column.Item().Element(item => MontarPagina(item, pagina, cultura));
                }
            });
    }

    private static void MontarPagina(IContainer container, PaginaInstanciaViewModel pagina, CultureInfo cultura)
    {
        container.Padding(12)
            .Background(Colors.Grey.Lighten4)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten3)
            .Column(column =>
            {
                column.Spacing(6);
                column.Item().Text(pagina.Titulo)
                    .FontSize(14)
                    .SemiBold();
                column.Item().Text(pagina.Concluida ? "Concluída" : "Pendente")
                    .FontColor(pagina.Concluida ? Colors.Green.Darken2 : Colors.Grey.Darken1);

                if (!pagina.Campos.Any())
                {
                    column.Item().Text("Nenhum campo registrado nesta página.");
                    return;
                }

                foreach (var campo in pagina.Campos)
                {
                    column.Item().Element(item => MontarCampo(item, campo, cultura));
                }
            });
    }

    private static void MontarCampo(IContainer container, CampoInstanciaViewModel campo, CultureInfo cultura)
    {
        var valor = ObterValorCampo(campo, cultura);
        var ajuda = string.IsNullOrWhiteSpace(campo.Ajuda) ? null : campo.Ajuda.Trim();

        container.BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingBottom(6)
            .Column(column =>
            {
                column.Spacing(3);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(campo.Rotulo ?? campo.NomeCampo).SemiBold();
                    row.ConstantItem(60).AlignRight().Text(campo.Obrigatorio ? "Obrigatório" : "Opcional")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                });

                if (!string.IsNullOrEmpty(ajuda))
                {
                    column.Item().Text(ajuda).FontSize(10).FontColor(Colors.Grey.Darken1);
                }

                column.Item().Background(Colors.White)
                    .Padding(8)
                    .Text(valor);
            });
    }

    private static string ObterValorCampo(CampoInstanciaViewModel campo, CultureInfo cultura)
    {
        if (!string.IsNullOrWhiteSpace(campo.ValorTexto))
            return campo.ValorTexto!;

        if (campo.ValorNumero.HasValue)
            return campo.ValorNumero.Value.ToString("N2", cultura);

        if (campo.ValorData.HasValue)
            return campo.ValorData.Value.ToString("dd/MM/yyyy", cultura);

        if (campo.ValorBool.HasValue)
            return campo.ValorBool.Value ? "Sim" : "Não";

        return "-";
    }
}
