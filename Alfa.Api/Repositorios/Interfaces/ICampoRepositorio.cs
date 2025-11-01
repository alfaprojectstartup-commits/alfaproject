using System.Collections.Generic;
using Alfa.Api.Dtos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface ICampoRepositorio
    {
        Task<IEnumerable<CampoModeloDto>> ListarPorPaginaModeloAsync(int empresaId, int paginaModeloId);
        Task<IEnumerable<CampoOpcaoDto>> ListarOpcoesAsync(int empresaId, int campoModeloId);
    }
}
