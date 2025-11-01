using System.Collections.Generic;

namespace Alfa.Api.Dtos
{
    public record FaseModeloDto(int Id, string Titulo, int Ordem, bool Ativo);

    public record FaseInstanciaDto(
        int Id,
        int FaseModeloId,
        string Titulo,
        int Ordem,
        string Status,
        int PorcentagemProgresso,
        IEnumerable<PaginaInstanciaDto> Paginas);
}
