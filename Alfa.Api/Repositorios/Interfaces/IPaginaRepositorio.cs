using Alfa.Api.Dtos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface IPaginaRepositorio
    {
        Task<IEnumerable<PaginaModelosDto>> ListarTemplatesPorFaseModelosAsync(int empresaId, int FaseModeloId);
        Task<IEnumerable<PaginaModelosDto>> ListarTemplatesPorFasesAsync(int empresaId, int FasesId);
    }
}
