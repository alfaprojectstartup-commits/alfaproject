using System;

namespace Alfa.Api.Dtos
{
    public record CampoModeloDto(
        int Id,
        int PaginaModeloId,
        string NomeCampo,
        string Rotulo,
        string Tipo,
        bool Obrigatorio,
        int Ordem,
        string? Placeholder,
        string? Mascara,
        string? Ajuda);

    public record CampoInstanciaDto(
        int Id,
        int CampoModeloId,
        string NomeCampo,
        string Rotulo,
        string Tipo,
        bool Obrigatorio,
        int Ordem,
        string? Placeholder,
        string? Mascara,
        string? Ajuda,
        string? ValorTexto,
        decimal? ValorNumero,
        DateTime? ValorData,
        bool? ValorBool);

    public record CampoOpcaoDto(int Id, int CampoModeloId, string Texto, string? Valor, int Ordem, bool Ativo);
}
