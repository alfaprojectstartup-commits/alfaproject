using System.Collections.Generic;

namespace Alfa.Api.Dtos
{
    public record PaginaModeloDto(int Id, int FaseModeloId, string Titulo, int Ordem);

    public record PaginaInstanciaDto(
        int Id,
        int PaginaModeloId,
        string Titulo,
        int Ordem,
        bool Concluida,
        IEnumerable<CampoInstanciaDto> Campos);
}
