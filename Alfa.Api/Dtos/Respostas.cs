namespace Alfa.Api.Dtos
{
    public record PaginaRespostaDto(int FasesId, int PaginaModelosId, List<FieldResponseDto> Campos);
    public record FieldResponseDto(int FieldTemplateId, string? ValorTexto, decimal? ValorNumero, DateTime? ValorData, bool? ValorBool);
}
