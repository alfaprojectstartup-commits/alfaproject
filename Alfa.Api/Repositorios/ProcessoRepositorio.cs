using Alfa.Api.Dtos;
using Alfa.Api.Infra.Interfaces;
using Alfa.Api.Repositorios.Interfaces;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

public class ProcessoRepositorio : IProcessoRepositorio
{
    private readonly IConexaoSql _db;
    public ProcessoRepositorio(IConexaoSql db) => _db = db;

    public async Task<(int total, IEnumerable<ProcessoListItemDto> items)> ListarAsync(
        int empresaId, int page, int pageSize, string? status)
    {
        using var conn = await _db.AbrirConexaoAsync();

        const string totalSql = @"
        SELECT COUNT(*)
        FROM Processos p
        JOIN ProcessoStatus ps ON ps.Id = p.StatusId
        WHERE p.EmpresaId = @EmpresaId
          AND (@Status IS NULL OR ps.Status = @Status);";

        var total = await conn.ExecuteScalarAsync<int>(totalSql, new
        {
            EmpresaId = empresaId,
            Status = status
        });

        const string itensSql = @"
        SELECT
            p.Id,
            p.Titulo,
            ps.Status,
            p.PorcentagemProgresso
        FROM Processos p
        JOIN ProcessoStatus ps ON ps.Id = p.StatusId
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

    public async Task<int> CriarAsync(int empresaId, string titulo, int[] faseModeloIds)
    {
        using var cn = await _db.AbrirConexaoAsync();
        using var tx = cn.BeginTransaction();

        var processoStatusId = await cn.ExecuteScalarAsync<int?>(
            "SELECT TOP 1 Id FROM ProcessoStatus WHERE EmpresaId=@empresaId AND Status=N'Em Andamento'",
            new { empresaId }, tx) ?? throw new InvalidOperationException("Status padrão de processo não encontrado.");

        var faseStatusId = await cn.ExecuteScalarAsync<int?>(
            "SELECT TOP 1 Id FROM FaseStatus WHERE EmpresaId=@empresaId AND Status=N'Em Andamento'",
            new { empresaId }, tx) ?? throw new InvalidOperationException("Status padrão de fase não encontrado.");

        var procId = await cn.ExecuteScalarAsync<int>(
            @"INSERT INTO Processos (EmpresaId, Titulo, StatusId)
              VALUES (@empresaId, @titulo, @statusId);
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new { empresaId, titulo, statusId = processoStatusId }, tx);

        if (faseModeloIds?.Length > 0)
        {
            var templateRows = (await cn.QueryAsync<TemplateRow>(@"
                SELECT
                    fm.Id            AS FaseModeloId,
                    fm.Titulo        AS FaseTitulo,
                    fm.Ordem         AS FaseOrdem,
                    pm.Id            AS PaginaModeloId,
                    pm.Titulo        AS PaginaTitulo,
                    pm.Ordem         AS PaginaOrdem,
                    cm.Id            AS CampoModeloId,
                    cm.NomeCampo     AS CampoNome,
                    cm.Rotulo        AS CampoRotulo,
                    cm.Tipo          AS CampoTipo,
                    cm.Obrigatorio   AS CampoObrigatorio,
                    cm.Ordem         AS CampoOrdem,
                    cm.Placeholder   AS CampoPlaceholder,
                    cm.Mascara       AS CampoMascara,
                    cm.Ajuda         AS CampoAjuda
                FROM FaseModelos fm
                LEFT JOIN PaginaModelos pm
                  ON pm.EmpresaId = fm.EmpresaId
                 AND pm.FaseModeloId = fm.Id
                LEFT JOIN CampoModelos cm
                  ON cm.EmpresaId = fm.EmpresaId
                 AND cm.PaginaModeloId = pm.Id
                WHERE fm.EmpresaId = @empresaId
                  AND fm.Id IN @ids
                ORDER BY fm.Ordem, pm.Ordem, cm.Ordem;",
                new { empresaId, ids = faseModeloIds }, tx)).ToList();

            var fasesAgrupadas = templateRows
                .GroupBy(r => new { r.FaseModeloId, r.FaseTitulo, r.FaseOrdem })
                .OrderBy(g => g.Key.FaseOrdem);

            foreach (var fase in fasesAgrupadas)
            {
                var faseInstanciaId = await cn.ExecuteScalarAsync<int>(
                    @"INSERT INTO FaseInstancias (EmpresaId, ProcessoId, FaseModeloId, Titulo, Ordem, StatusId)
                      VALUES (@empresaId, @processoId, @faseModeloId, @titulo, @ordem, @statusId);
                      SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new
                    {
                        empresaId,
                        processoId = procId,
                        faseModeloId = fase.Key.FaseModeloId,
                        titulo = fase.Key.FaseTitulo,
                        ordem = fase.Key.FaseOrdem,
                        statusId = faseStatusId
                    }, tx);

                var paginas = fase
                    .Where(r => r.PaginaModeloId.HasValue)
                    .GroupBy(r => new
                    {
                        r.PaginaModeloId,
                        r.PaginaTitulo,
                        r.PaginaOrdem
                    })
                    .OrderBy(g => g.Key.PaginaOrdem ?? int.MaxValue);

                foreach (var pagina in paginas)
                {
                    var paginaInstanciaId = await cn.ExecuteScalarAsync<int>(
                        @"INSERT INTO PaginaInstancias (EmpresaId, FaseInstanciaId, PaginaModeloId, Titulo, Ordem)
                          VALUES (@empresaId, @faseInstanciaId, @paginaModeloId, @titulo, @ordem);
                          SELECT CAST(SCOPE_IDENTITY() AS INT);",
                        new
                        {
                            empresaId,
                            faseInstanciaId,
                            paginaModeloId = pagina.Key.PaginaModeloId!.Value,
                            titulo = pagina.Key.PaginaTitulo!,
                            ordem = pagina.Key.PaginaOrdem ?? 0
                        }, tx);

                    var campos = pagina
                        .Where(r => r.CampoModeloId.HasValue)
                        .OrderBy(r => r.CampoOrdem ?? int.MaxValue);

                    foreach (var campo in campos)
                    {
                        await cn.ExecuteAsync(
                            @"INSERT INTO CampoInstancias
                                (EmpresaId, PaginaInstanciaId, CampoModeloId, NomeCampo, Rotulo, Tipo, Obrigatorio, Ordem, Placeholder, Mascara, Ajuda)
                              VALUES
                                (@empresaId, @paginaInstanciaId, @campoModeloId, @nomeCampo, @rotulo, @tipo, @obrigatorio, @ordem, @placeholder, @mascara, @ajuda);",
                            new
                            {
                                empresaId,
                                paginaInstanciaId,
                                campoModeloId = campo.CampoModeloId!.Value,
                                nomeCampo = campo.CampoNome!,
                                rotulo = campo.CampoRotulo!,
                                tipo = campo.CampoTipo!,
                                obrigatorio = campo.CampoObrigatorio ?? false,
                                ordem = campo.CampoOrdem ?? 0,
                                placeholder = campo.CampoPlaceholder,
                                mascara = campo.CampoMascara,
                                ajuda = campo.CampoAjuda
                            }, tx);
                    }
                }
            }
        }

        tx.Commit();
        return procId;
    }

    public async Task<ProcessoDetalheDto?> ObterAsync(int empresaId, int id)
    {
        using var cn = await _db.AbrirConexaoAsync();

        var proc = await cn.QueryFirstOrDefaultAsync<(int Id, string Titulo, string Status, int Progresso, DateTime CriadoEm)?>(
            @"SELECT p.Id, p.Titulo, ps.Status, p.PorcentagemProgresso, p.CriadoEm
              FROM Processos p
              JOIN ProcessoStatus ps ON ps.Id = p.StatusId
             WHERE p.EmpresaId=@empresaId AND p.Id=@id;",
            new { empresaId, id });

        if (proc is null) return null;

        var rows = (await cn.QueryAsync<ProcessoGraphRow>(@"
            SELECT
                fi.Id              AS FaseId,
                fi.FaseModeloId    AS FaseModeloId,
                fi.Titulo          AS FaseTitulo,
                fi.Ordem           AS FaseOrdem,
                fs.Status          AS FaseStatus,
                fi.PorcentagemProgresso AS FaseProgresso,
                pi.Id              AS PaginaId,
                pi.PaginaModeloId  AS PaginaModeloId,
                pi.Titulo          AS PaginaTitulo,
                pi.Ordem           AS PaginaOrdem,
                pi.Concluida       AS PaginaConcluida,
                ci.Id              AS CampoId,
                ci.CampoModeloId   AS CampoModeloId,
                ci.NomeCampo       AS CampoNome,
                ci.Rotulo          AS CampoRotulo,
                ci.Tipo            AS CampoTipo,
                ci.Obrigatorio     AS CampoObrigatorio,
                ci.Ordem           AS CampoOrdem,
                ci.Placeholder     AS CampoPlaceholder,
                ci.Mascara         AS CampoMascara,
                ci.Ajuda           AS CampoAjuda,
                ci.ValorTexto      AS ValorTexto,
                ci.ValorNumero     AS ValorNumero,
                ci.ValorData       AS ValorData,
                ci.ValorBool       AS ValorBool
            FROM FaseInstancias fi
            JOIN FaseStatus fs ON fs.Id = fi.StatusId
            LEFT JOIN PaginaInstancias pi
              ON pi.EmpresaId = fi.EmpresaId
             AND pi.FaseInstanciaId = fi.Id
            LEFT JOIN CampoInstancias ci
              ON ci.EmpresaId = fi.EmpresaId
             AND ci.PaginaInstanciaId = pi.Id
            WHERE fi.EmpresaId = @empresaId AND fi.ProcessoId = @id
            ORDER BY fi.Ordem, pi.Ordem, ci.Ordem;",
            new { empresaId, id })).ToList();

        var fases = rows
            .GroupBy(r => new
            {
                r.FaseId,
                r.FaseModeloId,
                r.FaseTitulo,
                r.FaseOrdem,
                r.FaseStatus,
                r.FaseProgresso
            })
            .OrderBy(g => g.Key.FaseOrdem)
            .Select(fase =>
            {
                var paginas = fase
                    .Where(r => r.PaginaId.HasValue)
                    .GroupBy(r => new
                    {
                        r.PaginaId,
                        r.PaginaModeloId,
                        r.PaginaTitulo,
                        r.PaginaOrdem,
                        r.PaginaConcluida
                    })
                    .OrderBy(p => p.Key.PaginaOrdem ?? int.MaxValue)
                    .Select(pagina => new PaginaInstanciaDto(
                        pagina.Key.PaginaId!.Value,
                        pagina.Key.PaginaModeloId ?? 0,
                        pagina.Key.PaginaTitulo ?? string.Empty,
                        pagina.Key.PaginaOrdem ?? 0,
                        pagina.Key.PaginaConcluida ?? false,
                        pagina
                            .Where(r => r.CampoId.HasValue)
                            .OrderBy(r => r.CampoOrdem ?? int.MaxValue)
                            .Select(c => new CampoInstanciaDto(
                                c.CampoId!.Value,
                                c.CampoModeloId ?? 0,
                                c.CampoNome ?? string.Empty,
                                c.CampoRotulo ?? string.Empty,
                                c.CampoTipo ?? string.Empty,
                                c.CampoObrigatorio ?? false,
                                c.CampoOrdem ?? 0,
                                c.CampoPlaceholder,
                                c.CampoMascara,
                                c.CampoAjuda,
                                c.ValorTexto,
                                c.ValorNumero,
                                c.ValorData,
                                c.ValorBool
                            ))
                    ));

                return new FaseInstanciaDto(
                    fase.Key.FaseId,
                    fase.Key.FaseModeloId,
                    fase.Key.FaseTitulo,
                    fase.Key.FaseOrdem,
                    fase.Key.FaseStatus,
                    fase.Key.FaseProgresso,
                    paginas.ToList());
            })
            .ToList();

        return new ProcessoDetalheDto(proc.Value.Id, proc.Value.Titulo, proc.Value.Status, proc.Value.Progresso, proc.Value.CriadoEm, fases);
    }

    public async Task AtualizarStatusEProgressoAsync(int empresaId, int processoId, string? status, int? progresso)
    {
        if (status is null && progresso is null) return;

        using var cn = await _db.AbrirConexaoAsync();

        int? statusId = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            statusId = await cn.ExecuteScalarAsync<int?>(
                "SELECT Id FROM ProcessoStatus WHERE EmpresaId=@empresaId AND Status=@status",
                new { empresaId, status });
        }

        var setClauses = new List<string>();
        var parameters = new DynamicParameters(new { empresaId, processoId });

        if (statusId.HasValue)
        {
            setClauses.Add("StatusId=@StatusId");
            parameters.Add("StatusId", statusId.Value);
        }

        if (progresso.HasValue)
        {
            setClauses.Add("PorcentagemProgresso=@Progresso");
            parameters.Add("Progresso", progresso.Value);
        }

        if (setClauses.Count == 0) return;

        var sql = $"UPDATE Processos SET {string.Join(",", setClauses)} WHERE EmpresaId=@empresaId AND Id=@processoId";
        await cn.ExecuteAsync(sql, parameters);
    }

    private record TemplateRow(
        int FaseModeloId,
        string FaseTitulo,
        int FaseOrdem,
        int? PaginaModeloId,
        string? PaginaTitulo,
        int? PaginaOrdem,
        int? CampoModeloId,
        string? CampoNome,
        string? CampoRotulo,
        string? CampoTipo,
        bool? CampoObrigatorio,
        int? CampoOrdem,
        string? CampoPlaceholder,
        string? CampoMascara,
        string? CampoAjuda);

    private record ProcessoGraphRow(
        int FaseId,
        int FaseModeloId,
        string FaseTitulo,
        int FaseOrdem,
        string FaseStatus,
        int FaseProgresso,
        int? PaginaId,
        int? PaginaModeloId,
        string? PaginaTitulo,
        int? PaginaOrdem,
        bool? PaginaConcluida,
        int? CampoId,
        int? CampoModeloId,
        string? CampoNome,
        string? CampoRotulo,
        string? CampoTipo,
        bool? CampoObrigatorio,
        int? CampoOrdem,
        string? CampoPlaceholder,
        string? CampoMascara,
        string? CampoAjuda,
        string? ValorTexto,
        decimal? ValorNumero,
        DateTime? ValorData,
        bool? ValorBool);
}
