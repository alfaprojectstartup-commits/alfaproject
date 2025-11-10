using Alfa.Api.Dtos;

namespace Alfa.Api.Aplicacao.Interfaces
{
    public interface IProcessoApp
    {
        Task<(int total, IEnumerable<ProcessoListItemDto> items)> Listar(int empresaId, int page, int pageSize, string? status);
        Task<int> Criar(int empresaId, ProcessoCriarDto dto);
        Task<ProcessoDetalheDto?> Obter(int empresaId, int id);
        Task<IEnumerable<FaseInstanciaDto>> ListarFases(int empresaId, int processoId);
        Task RegistrarResposta(int processoId, PaginaRespostaDto dto);
        Task<int?> ObterProcessoIdDaFase(int faseInstanciaId);
        Task RecalcularProgressoProcesso(int empresaId, int processoId);
        Task<IEnumerable<ProcessoPadraoModeloDto>> ListarPadroesAsync(int empresaId);
        Task<int> CriarPadraoAsync(int empresaId, ProcessoPadraoModeloInputDto dto);
        Task AtualizarStatus(int empresaId, int processoId, string status, int? usuarioId, string? usuarioNome);
        Task<IEnumerable<ProcessoHistoricoDto>> ListarHistoricos(int empresaId, int processoId);
    }
}
