using System.Collections.Generic;
using Alfa.Api.Dtos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface IRespostaRepositorio
    {
        Task SalvarPaginaAsync(int empresaId, PaginaRespostaDto dto);
        Task<int> ContarCamposObrigatoriosAsync(int empresaId, int paginaInstanciaId);
        Task<int> ContarCamposPreenchidosAsync(int empresaId, int paginaInstanciaId);
        Task<int> ContarPaginasDaFaseAsync(int empresaId, int faseInstanciaId);
        Task<IEnumerable<(int paginaInstanciaId, int obrig, int preenc)>> ObterResumoPorPaginasDaFaseAsync(int empresaId, int faseInstanciaId);
    }
}
