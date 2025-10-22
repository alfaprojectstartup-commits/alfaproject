using Alfa.Api.Dtos;

namespace Alfa.Api.Aplicacao.Interfaces
{
    public interface IProcessoApp
    {
        Task<(int total, IEnumerable<ProcessoListItemDto> items)> Listar(int empresaId, int page, int pageSize, string? status);
        Task<int> Criar(int empresaId, ProcessoCreateDto dto);
        Task<ProcessoDetalheDto?> Obter(int empresaId, int id);
        Task RecalcularProgressoProcesso(int empresaId, int processoId);
    }
}
