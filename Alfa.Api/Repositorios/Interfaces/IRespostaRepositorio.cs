using Alfa.Api.Dtos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface IRespostaRepositorio
    {
        Task SalvarPaginaAsync(int empresaId, PaginaRespostaDto dto);
        Task<int> ContarCamposObrigatoriosAsync(int empresaId, int PaginaModelosId);
        Task<int> ContarCamposPreenchidosAsync(int empresaId, int FasesId, int PaginaModelosId);
        Task<int> ContarPaginasDaFaseAsync(int empresaId, int FasesId);
        Task<IEnumerable<(int paginaId, int obrig, int preenc)>> ObterResumoPorPaginasDaFaseAsync(int empresaId, int FasesId);
    }
}
