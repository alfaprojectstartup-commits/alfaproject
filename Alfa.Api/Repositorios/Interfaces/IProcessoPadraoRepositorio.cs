using Alfa.Api.Dtos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface IProcessoPadraoRepositorio
    {
        Task<IEnumerable<ProcessoPadraoModeloDto>> ListarAsync(int empresaId);
        Task<int> CriarAsync(int empresaId, ProcessoPadraoModeloInputDto dto);
    }
}
