using Alfa.Api.Dtos;

namespace Alfa.Api.Aplicacao;
public interface IProcessoApp
{
    Task<Paginado<ProcessoListaDto>> ListarAsync(int empresaId, int pagina, int tamanho, CancellationToken ct);
    Task<int> CriarAsync(ProcessoCriarDto dto, CancellationToken ct);
    Task<bool> AtualizarTituloAsync(int id, int empresaId, ProcessoAtualizarTituloDto dto, CancellationToken ct);
    Task<bool> ExcluirAsync(int id, int empresaId, CancellationToken ct);
}