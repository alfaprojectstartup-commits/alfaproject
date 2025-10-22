namespace Alfa.Api.Dtos
{
    public record PageResponseDto(int FaseInstanceId, int PaginaTemplateId, List<FieldResponseDto> Campos);
    public record FieldResponseDto(int FieldTemplateId, string? ValorTexto, decimal? ValorNumero, DateTime? ValorData, bool? ValorBool);
}
