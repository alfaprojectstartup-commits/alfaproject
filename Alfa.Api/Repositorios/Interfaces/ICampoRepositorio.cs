using Alfa.Api.Dtos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface ICampoRepositorio
    {
        Task<IEnumerable<CampoTemplateDto>> ListarPorPaginaAsync(int empresaId, int paginaTemplateId);
        Task<IEnumerable<CampoOpcaoDto>> ListarOpcoesAsync(int empresaId, int fieldTemplateId);
    }
}
