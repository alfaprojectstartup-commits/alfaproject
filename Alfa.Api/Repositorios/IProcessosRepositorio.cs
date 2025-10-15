using Alfa.Api.Dtos;
using Alfa.Api.Modelos;

namespace Alfa.Api.Repositorios;
public interface IProcessoRepositorio
{
    Task<Paginado<ProcessoListaDto>> ListarAsync(int empresaId, int pagina, int tamanho, CancellationToken ct);
    Task<int> CriarAsync(Processo p, CancellationToken ct);
    Task<Processo?> ObterAsync(int id, int empresaId, CancellationToken ct);
    Task<bool> AtualizarTituloAsync(int id, int empresaId, string titulo, CancellationToken ct);
    Task<bool> ExcluirAsync(int id, int empresaId, CancellationToken ct);
}