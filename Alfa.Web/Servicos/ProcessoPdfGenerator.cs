using System.Globalization;
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
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Column(column =>
                {
                    column.Spacing(2);
                    column.Item().Text(processo.Titulo)
                        .FontSize(20)
                        .SemiBold();
                    column.Item().Text(text =>
                    {
                        text.Span("Status: ").SemiBold();
                        text.Span(processo.Status);
                    });
                    column.Item().Text(text =>
                    {
                        text.Span("Criado em: ").SemiBold();
                        text.Span(processo.CriadoEm.ToString("dd/MM/yyyy HH:mm", cultura));
                    });
                });

                page.Content().Column(column =>
                {
                    column.Spacing(15);

                    if (!fases.Any())
                    {
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Text("Nenhuma fase disponível.");
                        return;
                    }

                    foreach (var fase in fases)
                    {
                        column.Item().Element(container => MontarFase(container, fase, cultura));
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                    text.Span(" · Gerado em ");
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm", cultura));
                });
            });
        });

        return documento.GeneratePdf();
    }

    private static void MontarFase(IContainer container, FaseInstanciaViewModel fase, CultureInfo cultura)
    {
        container.Border(1)
            .BorderColor(Colors.Grey.Medium)
            .Padding(12)
            .Column(column =>
            {
                column.Spacing(8);
                column.Item().Text(fase.Titulo)
                    .FontSize(16)
                    .SemiBold();
                column.Item().Text(text =>
                {
                    text.Span("Status: ").SemiBold();
                    text.Span(fase.Status);
                    text.Span(" · Progresso: ");
                    text.Span($"{fase.PorcentagemProgresso}%");
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
        container.Padding(10)
            .Background(Colors.Grey.Lighten4)
            .Column(column =>
            {
                column.Spacing(6);
                column.Item().Text(pagina.Titulo)
                    .FontSize(14)
                    .SemiBold();
                column.Item().Text(text =>
                {
                    text.Span("Status: ").SemiBold();
                    text.Span(pagina.Concluida ? "Concluída" : "Pendente");
                });

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
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingBottom(6)
            .Column(column =>
            {
                column.Spacing(3);
                column.Item().Text(campo.Rotulo ?? campo.NomeCampo).SemiBold();

                if (!string.IsNullOrEmpty(ajuda))
                {
                    column.Item().Text(ajuda).FontSize(10).FontColor(Colors.Grey.Darken1);
                }

                column.Item().Text(valor);
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
