using Alfa.Api.Dtos;
using Alfa.Api.Infra.Interfaces;
using Alfa.Api.Repositorios.Interfaces;
using Dapper;
using System.Data;

public class ProcessoRepositorio : IProcessoRepositorio
{
    private readonly IConexaoSql _db;
    public ProcessoRepositorio(IConexaoSql db) => _db = db;

    public async Task<(int total, IEnumerable<ProcessoListItemDto> items)> ListarAsync(
        int empresaId, int page, int pageSize, string? status)
    {
        using var conn = await _db.AbrirConexaoAsync();

        // total (com filtro de status se vier)
        var totalSql = @"
        SELECT COUNT(*)
        FROM Processos p
        JOIN ProcessoStatus ps ON ps.Id = p.Status
        WHERE p.EmpresaId = @EmpresaId
          AND (@Status IS NULL OR ps.Status = @Status);";

        var total = await conn.ExecuteScalarAsync<int>(totalSql, new
        {
            EmpresaId = empresaId,
            Status = status
        });

        // itens paginados — note os aliases iguais ao DTO
        var itensSql = @"
        SELECT
            p.Id,
            p.Titulo,
            ps.Status,
            CAST(0 AS INT) AS PorcentagemProgresso  -- placeholder (calcularemos depois)
        FROM Processos p
        JOIN ProcessoStatus ps ON ps.Id = p.Status
        WHERE p.EmpresaId = @EmpresaId
          AND (@Status IS NULL OR ps.Status = @Status)
        ORDER BY p.Id DESC
        OFFSET (@Page - 1) * @PageSize ROWS
        FETCH NEXT @PageSize ROWS ONLY;";

        var items = await conn.QueryAsync<ProcessoListItemDto>(itensSql, new
        {
            EmpresaId = empresaId,
            Status = status,
            Page = page < 1 ? 1 : page,
            PageSize = pageSize < 1 ? 10 : pageSize
        });

        return (total, items);
    }

    public async Task<int> CriarAsync(int empresaId, string titulo, int[] fasesTemplateIds)
    {
        using var cn = await _db.AbrirConexaoAsync();
        using var tx = cn.BeginTransaction();

        var procId = await cn.ExecuteScalarAsync<int>(
            @"INSERT INTO Processos (EmpresaId, Titulo) VALUES (@empresaId, @titulo);
              SELECT SCOPE_IDENTITY();", new { empresaId, titulo }, tx);

        // cria instâncias de fase seguindo a ordem do template
        var fases = await cn.QueryAsync<(int Id, int Ordem, string Nome)>(
            @"SELECT Id, Ordem, Nome FROM Fases WHERE EmpresaId=@empresaId AND Id IN @ids ORDER BY Ordem",
            new { empresaId, ids = fasesTemplateIds }, tx);

        var ordem = 1;
        foreach (var f in fases)
        {
            await cn.ExecuteAsync(
                @"INSERT INTO Fases (EmpresaId, ProcessInstanceId, FaseModeloId, NomeFase, Ordem)
                  VALUES (@empresaId, @procId, @faseTId, @nome, @ordem)",
                new { empresaId, procId, faseTId = f.Id, nome = f.Nome, ordem }, tx);
        }

        tx.Commit();
        return procId;
    }

    public async Task<ProcessoDetalheDto?> ObterAsync(int empresaId, int id)
    {
        using var cn = await _db.AbrirConexaoAsync();

        var proc = await cn.QueryFirstOrDefaultAsync<(int Id, string Titulo, string Status, int PorcentagemProgresso)>(
            @"SELECT Id, Titulo, Status, PorcentagemProgresso FROM Processos WHERE EmpresaId=@empresaId AND Id=@id",
            new { empresaId, id });
        if (proc.Id == 0) return null;

        var fases = await cn.QueryAsync<FasesDto>(
            @"SELECT Id, Ordem, NomeFase, Status, PorcentagemProgresso
              FROM Fases WHERE EmpresaId=@empresaId AND ProcessInstanceId=@id ORDER BY Ordem",
            new { empresaId, id });

        return new ProcessoDetalheDto(proc.Id, proc.Titulo, proc.Status, proc.PorcentagemProgresso, fases);
    }

    public async Task AtualizarStatusEProgressoAsync(int empresaId, int processoId, string? status, int? progresso)
    {
        using var cn = await _db.AbrirConexaoAsync();
        var set = new List<string>();
        if (status != null) set.Add("Status=@status");
        if (progresso != null) set.Add("PorcentagemProgresso=@progresso");
        var sql = $"UPDATE Processos SET {string.Join(",", set)} WHERE EmpresaId=@empresaId AND Id=@processoId";
        await cn.ExecuteAsync(sql, new { empresaId, processoId, status, progresso });
    }
}
