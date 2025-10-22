using Alfa.Api.Dtos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface IFaseRepositorio
    {
        Task<IEnumerable<FaseTemplateDto>> ListarTemplatesAsync(int empresaId);
        Task<IEnumerable<FaseInstanceDto>> ListarInstanciasAsync(int empresaId, int processoId);
        Task RecalcularProgressoFaseAsync(int empresaId, int faseInstanceId);
    }
}
