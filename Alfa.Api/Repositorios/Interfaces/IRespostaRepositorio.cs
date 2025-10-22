using Alfa.Api.Dtos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface IRespostaRepositorio
    {
        Task SalvarPaginaAsync(int empresaId, PageResponseDto dto);
        Task<int> ContarCamposObrigatoriosAsync(int empresaId, int paginaTemplateId);
        Task<int> ContarCamposPreenchidosAsync(int empresaId, int faseInstanceId, int paginaTemplateId);
        Task<int> ContarPaginasDaFaseAsync(int empresaId, int faseInstanceId);
        Task<IEnumerable<(int paginaId, int obrig, int preenc)>> ObterResumoPorPaginasDaFaseAsync(int empresaId, int faseInstanceId);
    }
}
