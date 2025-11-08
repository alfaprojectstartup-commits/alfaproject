using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Alfa.Web.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Alfa.Web.Servicos;

public static class ProcessoPdfGenerator
{
    static ProcessoPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Gerar(ProcessoDetalheViewModel processo)
    {
        if (processo is null)
        {
            throw new ArgumentNullException(nameof(processo));
        }

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.DefaultTextStyle(TextStyle.Default.FontFamily("Helvetica"));

                page.Header().Column(column =>
                {
                    column.Item().Text("Relatório do Processo").FontSize(18).SemiBold();
                    column.Item().Text(processo.Titulo).FontSize(14);
                    column.Item().Text(text =>
                    {
                        text.Span("Status: ").SemiBold();
                        text.Span(processo.Status);
                        text.Span(" • ");
                        text.Span("Criado em: ").SemiBold();
                        text.Span(processo.CriadoEm.ToString("dd/MM/yyyy HH:mm"));
                        text.Span(" • ");
                        text.Span("Progresso: ").SemiBold();
                        text.Span($"{processo.PorcentagemProgresso}%");
                    }).FontSize(11).FontColor(Colors.Grey.Darken2);
                });

                page.Content().PaddingTop(20).Column(column =>
                {
                    if (processo.Fases == null || !processo.Fases.Any())
                    {
                        column.Item().Text("Nenhuma fase cadastrada para este processo.");
                        return;
                    }

                    foreach (var fase in processo.Fases)
                    {
                        column.Item().PaddingBottom(15).Element(section =>
                        {
                            section.BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            section.Column(faseCol =>
                            {
                                faseCol.Item().Text(fase.Titulo).FontSize(15).SemiBold();
                                faseCol.Item().Text(text =>
                                {
                                    text.Span("Status: ").SemiBold();
                                    text.Span(fase.Status);
                                    text.Span(" • ");
                                    text.Span("Progresso: ").SemiBold();
                                    text.Span($"{fase.PorcentagemProgresso}%");
                                }).FontSize(11).FontColor(Colors.Grey.Darken2);

                                if (fase.Paginas == null || !fase.Paginas.Any())
                                {
                                    faseCol.Item().PaddingTop(5).Text("Nenhuma página nesta fase.").Italic().FontColor(Colors.Grey.Darken1);
                                    return;
                                }

                                foreach (var pagina in fase.Paginas)
                                {
                                    faseCol.Item().PaddingTop(10).Element(paginaContainer =>
                                    {
                                        paginaContainer.Column(paginaCol =>
                                        {
                                            paginaCol.Item().Text(pagina.Titulo).FontSize(13).SemiBold();
                                            paginaCol.Item().Text(pagina.Concluida ? "Página concluída" : "Página pendente")
                                                .FontSize(10)
                                                .FontColor(pagina.Concluida ? Colors.Green.Darken2 : Colors.Grey.Darken2);

                                            if (pagina.Campos == null || !pagina.Campos.Any())
                                            {
                                                paginaCol.Item().PaddingTop(4).Text("Nenhum campo nesta página.").Italic().FontSize(10);
                                                return;
                                            }

                                            paginaCol.Item().PaddingTop(5).Table(table =>
                                            {
                                                table.ColumnsDefinition(columns =>
                                                {
                                                    columns.RelativeColumn(2);
                                                    columns.RelativeColumn(3);
                                                });

                                                table.Header(header =>
                                                {
                                                    header.Cell().Element(CellHeader).Text("Campo");
                                                    header.Cell().Element(CellHeader).Text("Valor informado");
                                                });

                                                foreach (var campo in pagina.Campos)
                                                {
                                                    table.Cell().Element(CellCampoNome).Text(campo.Rotulo + (campo.Obrigatorio ? " *" : string.Empty));
                                                    table.Cell().Element(CellCampoValor).Text(FormatarValor(campo));
                                                }
                                            });
                                        });
                                    });
                                }
                            });
                        });
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Gerado em ").FontSize(9);
                    x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                });
            });
        });

        using var stream = new MemoryStream();
        documento.GeneratePdf(stream);
        return stream.ToArray();
    }

    private static string FormatarValor(CampoInstanciaViewModel campo)
    {
        if (campo is null)
        {
            return string.Empty;
        }

        var tipo = (campo.Tipo ?? string.Empty).ToLowerInvariant();
        return tipo switch
        {
            "number" => campo.ValorNumero?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty,
            "date" => campo.ValorData?.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture) ?? string.Empty,
            "checkbox" => campo.ValorBool.HasValue ? (campo.ValorBool.Value ? "Sim" : "Não") : string.Empty,
            _ => string.IsNullOrWhiteSpace(campo.ValorTexto) ? string.Empty : campo.ValorTexto
        };
    }

    private static IContainer CellHeader(IContainer container)
        => container.DefaultTextStyle(TextStyle.Default.SemiBold().FontSize(11))
            .PaddingVertical(4)
            .Background(Colors.Grey.Lighten3)
            .PaddingHorizontal(6);

    private static IContainer CellCampoNome(IContainer container)
        => container.DefaultTextStyle(TextStyle.Default.FontSize(10))
            .PaddingHorizontal(6)
            .PaddingVertical(4);

    private static IContainer CellCampoValor(IContainer container)
        => container.DefaultTextStyle(TextStyle.Default.FontSize(10))
            .PaddingHorizontal(6)
            .PaddingVertical(4);
}
