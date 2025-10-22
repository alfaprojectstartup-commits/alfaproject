using Alfa.Api.Dados;
using Alfa.Api.Dtos;
using Alfa.Api.Repositorios.Interfaces;
using Dapper;
using System.Data;

public class ProcessoRepositorio : IProcessoRepositorio
{
    private readonly IConexaoSql _db;
    public ProcessoRepositorio(IConexaoSql db) => _db = db;

    public async Task<(int total, IEnumerable<ProcessoListItemDto> items)> ListarAsync(int empresaId, int page, int pageSize, string? status)
    {
        using var cn = await _db.AbrirAsync();
        var where = "WHERE EmpresaId=@empresaId";
        if (!string.IsNullOrEmpty(status)) where += " AND Status=@status";

        var total = await cn.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM ProcessInstances {where}", new { empresaId, status });
        var items = await cn.QueryAsync<ProcessoListItemDto>($@"
            SELECT Id, Titulo, Status, ProgressoPct, CriadoEm
              FROM ProcessInstances
              {where}
              ORDER BY Id DESC
              OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY",
            new { empresaId, status, skip = (page - 1) * pageSize, take = pageSize });

        return (total, items);
    }

    public async Task<int> CriarAsync(int empresaId, string titulo, int[] fasesTemplateIds)
    {
        using var cn = await _db.AbrirAsync();
        using var tx = cn.BeginTransaction();

        var procId = await cn.ExecuteScalarAsync<int>(
            @"INSERT INTO ProcessInstances (EmpresaId, Titulo) VALUES (@empresaId, @titulo);
              SELECT SCOPE_IDENTITY();", new { empresaId, titulo }, tx);

        // cria instâncias de fase seguindo a ordem do template
        var fases = await cn.QueryAsync<(int Id, int Ordem, string Nome)>(
            @"SELECT Id, Ordem, Nome FROM PhaseTemplates WHERE EmpresaId=@empresaId AND Id IN @ids ORDER BY Ordem",
            new { empresaId, ids = fasesTemplateIds }, tx);

        var ordem = 1;
        foreach (var f in fases)
        {
            await cn.ExecuteAsync(
                @"INSERT INTO PhaseInstances (EmpresaId, ProcessInstanceId, PhaseTemplateId, NomeFase, Ordem)
                  VALUES (@empresaId, @procId, @faseTId, @nome, @ordem)",
                new { empresaId, procId, faseTId = f.Id, nome = f.Nome, ordem }, tx);
        }

        tx.Commit();
        return procId;
    }

    public async Task<ProcessoDetalheDto?> ObterAsync(int empresaId, int id)
    {
        using var cn = await _db.AbrirAsync();

        var proc = await cn.QueryFirstOrDefaultAsync<(int Id, string Titulo, string Status, int ProgressoPct)>(
            @"SELECT Id, Titulo, Status, ProgressoPct FROM ProcessInstances WHERE EmpresaId=@empresaId AND Id=@id",
            new { empresaId, id });
        if (proc.Id == 0) return null;

        var fases = await cn.QueryAsync<FaseInstanceDto>(
            @"SELECT Id, Ordem, NomeFase, Status, ProgressoPct
              FROM PhaseInstances WHERE EmpresaId=@empresaId AND ProcessInstanceId=@id ORDER BY Ordem",
            new { empresaId, id });

        return new ProcessoDetalheDto(proc.Id, proc.Titulo, proc.Status, proc.ProgressoPct, fases);
    }

    public async Task AtualizarStatusEProgressoAsync(int empresaId, int processoId, string? status, int? progresso)
    {
        using var cn = await _db.AbrirAsync();
        var set = new List<string>();
        if (status != null) set.Add("Status=@status");
        if (progresso != null) set.Add("ProgressoPct=@progresso");
        var sql = $"UPDATE ProcessInstances SET {string.Join(",", set)} WHERE EmpresaId=@empresaId AND Id=@processoId";
        await cn.ExecuteAsync(sql, new { empresaId, processoId, status, progresso });
    }
}
