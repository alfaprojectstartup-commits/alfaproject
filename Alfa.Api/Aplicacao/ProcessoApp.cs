using Alfa.Api.Dtos;
using Alfa.Api.Modelos;
using Alfa.Api.Repositorios;
using System.Transactions;

namespace Alfa.Api.Aplicacao;
public class ProcessoApp(IProcessoRepositorio repo) : IProcessoApp
{
    public Task<Paginado<ProcessoListaDto>> ListarAsync(int empresaId, int pagina, int tamanho, CancellationToken ct)
        => repo.ListarAsync(empresaId, pagina, tamanho, ct);

    public async Task<int> CriarAsync(ProcessoCriarDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo))
            throw new ArgumentException("Título é obrigatório.");

        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        var id = await repo.CriarAsync(new Processo
        {
            Titulo = dto.Titulo.Trim(),
            EmpresaId = dto.EmpresaId,
            Status = "EmAndamento",
            ProgressoPct = 0
        }, ct);
        scope.Complete();
        return id;
    }

    public async Task<bool> AtualizarTituloAsync(int id, int empresaId, ProcessoAtualizarTituloDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo))
            throw new ArgumentException("Título é obrigatório.");

        return await repo.AtualizarTituloAsync(id, empresaId, dto.Titulo.Trim(), ct);
    }

    public Task<bool> ExcluirAsync(int id, int empresaId, CancellationToken ct)
        => repo.ExcluirAsync(id, empresaId, ct);
}