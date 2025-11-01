using System.Collections.Generic;
using Alfa.Api.Dtos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface IFaseRepositorio
    {
        Task<IEnumerable<FaseModeloDto>> ListarTemplatesAsync(int empresaId);
        Task<IEnumerable<FaseInstanciaDto>> ListarInstanciasAsync(int empresaId, int processoId);
        Task RecalcularProgressoFaseAsync(int empresaId, int faseInstanciaId);
    }
}
