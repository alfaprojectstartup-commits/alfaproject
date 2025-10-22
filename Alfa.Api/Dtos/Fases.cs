namespace Alfa.Api.Dtos
{
    public record FaseTemplateDto(int Id, string Nome, int Ordem, bool Ativo);
    public record FaseInstanceDto(int Id, int Ordem, string NomeFase, string Status, int ProgressoPct);

}
