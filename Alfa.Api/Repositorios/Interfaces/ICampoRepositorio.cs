using Alfa.Api.Dtos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface ICampoRepositorio
    {
        Task<IEnumerable<CampoModeloDto>> ListarPorPaginaAsync(int empresaId, int PaginaModelosId);
        Task<IEnumerable<CampoOpcaoDto>> ListarOpcoesAsync(int empresaId, int fieldTemplateId);
    }
}
