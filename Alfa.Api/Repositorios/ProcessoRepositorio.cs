using Alfa.Api.Dados;
using Alfa.Api.Dtos;
using Alfa.Api.Modelos;
using Dapper;

namespace Alfa.Api.Repositorios;
public class ProcessoRepositorio(IConexaoSql conexao) : IProcessoRepositorio
{
    public async Task<Paginado<ProcessoListaDto>> ListarAsync(int empresaId, int pagina, int tamanho, CancellationToken ct)
    {
        using var cnn = await conexao.AbrirAsync(ct);
        var sql = """
            SELECT Id, Titulo, Status, ProgressoPct, CriadoEm
            FROM ProcessInstances
            WHERE EmpresaId = @empresaId
            ORDER BY Id DESC
            OFFSET (@pagina-1)*@tamanho ROWS FETCH NEXT @tamanho ROWS ONLY;

            SELECT COUNT(1) FROM ProcessInstances WHERE EmpresaId = @empresaId;
        """;
        using var multi = await cnn.QueryMultipleAsync(sql, new { empresaId, pagina, tamanho });
        var itens = (await multi.ReadAsync<ProcessoListaDto>()).ToList();
        var total = await multi.ReadSingleAsync<int>();
        return new Paginado<ProcessoListaDto>(itens, total, pagina, tamanho);
    }

    public async Task<int> CriarAsync(Processo p, CancellationToken ct)
    {
        using var cnn = await conexao.AbrirAsync(ct);
        var sql = """
            INSERT INTO ProcessInstances (Titulo, Status, ProgressoPct, EmpresaId, CriadoEm)
            VALUES (@Titulo, @Status, @ProgressoPct, @EmpresaId, SYSDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS int);
        """;
        return await cnn.ExecuteScalarAsync<int>(sql, p);
    }

    public async Task<Processo?> ObterAsync(int id, int empresaId, CancellationToken ct)
    {
        using var cnn = await conexao.AbrirAsync(ct);
        var sql = "SELECT TOP 1 * FROM ProcessInstances WHERE Id=@id AND EmpresaId=@empresaId";
        return await cnn.QueryFirstOrDefaultAsync<Processo>(sql, new { id, empresaId });
    }

    public async Task<bool> AtualizarTituloAsync(int id, int empresaId, string titulo, CancellationToken ct)
    {
        using var cnn = await conexao.AbrirAsync(ct);
        var sql = "UPDATE ProcessInstances SET Titulo=@titulo WHERE Id=@id AND EmpresaId=@empresaId";
        var linhas = await cnn.ExecuteAsync(sql, new { id, empresaId, titulo });
        return linhas > 0;
    }

    public async Task<bool> ExcluirAsync(int id, int empresaId, CancellationToken ct)
    {
        using var cnn = await conexao.AbrirAsync(ct);
        var sql = "DELETE FROM ProcessInstances WHERE Id=@id AND EmpresaId=@empresaId";
        var linhas = await cnn.ExecuteAsync(sql, new { id, empresaId });
        return linhas > 0;
    }
}