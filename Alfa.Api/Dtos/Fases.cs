namespace Alfa.Api.Dtos
{
    public record FaseModelosDto(int Id, string Nome, int Ordem, bool Ativo);
    public record FasesDto(int Id, int Ordem, string NomeFase, string Status, int PorcentagemProgresso);

}
