using Alfa.Api.Dtos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface IFaseRepositorio
    {
        Task<IEnumerable<FaseModelosDto>> ListarTemplatesAsync(int empresaId);
        Task<IEnumerable<FasesDto>> ListarInstanciasAsync(int empresaId, int processoId);
        Task RecalcularProgressoFaseAsync(int empresaId, int FasesId);
    }
}
