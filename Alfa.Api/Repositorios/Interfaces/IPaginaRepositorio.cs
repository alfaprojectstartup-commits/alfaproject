using System.Collections.Generic;
using Alfa.Api.Dtos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface IPaginaRepositorio
    {
        Task<IEnumerable<PaginaModeloDto>> ListarTemplatesPorFaseModeloAsync(int empresaId, int faseModeloId);
        Task<IEnumerable<PaginaModeloDto>> ListarTemplatesPorFaseInstanciaAsync(int empresaId, int faseInstanciaId);
    }
}
