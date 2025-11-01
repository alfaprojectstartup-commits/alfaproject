using System.Collections.Generic;
using System.Threading.Tasks;
using Alfa.Api.Dtos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface IFaseRepositorio
    {
        Task<IEnumerable<FaseModeloDto>> ListarTemplatesAsync(int empresaId);
        Task<FaseModeloDto?> ObterTemplateAsync(int empresaId, int faseModeloId);
        Task<int> CriarTemplateAsync(int empresaId, FaseModeloInputDto dto);
        Task AtualizarTemplateAsync(int empresaId, int faseModeloId, FaseModeloInputDto dto);
        Task<IEnumerable<FaseInstanciaDto>> ListarInstanciasAsync(int empresaId, int processoId);
        Task RecalcularProgressoFaseAsync(int empresaId, int faseInstanciaId);
    }
}
