using System.Collections.Generic;
using Alfa.Api.Dtos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface IProcessoRepositorio
    {
        Task<(int total, IEnumerable<ProcessoListItemDto> items)> ListarAsync(int empresaId, int page, int pageSize, string? status);
        Task<int> CriarAsync(int empresaId, string titulo, int[] faseModeloIds);
        Task<ProcessoDetalheDto?> ObterAsync(int empresaId, int id);
        Task<(int empresaId, int processoId)?> ObterEmpresaEProcessoDaFaseAsync(int faseInstanciaId);
        Task AtualizarStatusEProgressoAsync(int empresaId, int processoId, string? status, int? progresso);
    }
}
