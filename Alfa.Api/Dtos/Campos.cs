namespace Alfa.Api.Dtos
{
    public record CampoTemplateDto(int Id, string NomeCampo, string Rotulo, string Tipo, bool Obrigatorio, int Ordem);
    public record CampoOpcaoDto(int Id, string Texto, string? Valor, int Ordem, bool Ativo);

}
