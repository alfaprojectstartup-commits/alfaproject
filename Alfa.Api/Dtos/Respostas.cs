using System;
using System.Collections.Generic;

namespace Alfa.Api.Dtos
{
    public record PaginaRespostaDto(int FaseInstanciaId, int PaginaInstanciaId, List<FieldResponseDto> Campos);

    public record FieldResponseDto(
        int CampoInstanciaId,
        string? ValorTexto,
        decimal? ValorNumero,
        DateTime? ValorData,
        bool? ValorBool);
}
