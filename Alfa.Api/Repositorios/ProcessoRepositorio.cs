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
            p.PorcentagemProgresso,
            p.CriadoEm,
            hist.UsuariosConcatenados
        FROM Processos p
        JOIN ProcessoStatus ps ON ps.Id = p.StatusId
        OUTER APPLY (
            SELECT STRING_AGG(n.UsuarioNome, '||') WITHIN GROUP (ORDER BY n.UsuarioNome) AS UsuariosConcatenados
            FROM (
                SELECT DISTINCT COALESCE(u.Nome, ph.UsuarioNome) AS UsuarioNome
                FROM ProcessoHistoricos ph
                LEFT JOIN Usuarios u
                       ON u.Id = ph.UsuarioId
                WHERE ph.EmpresaId = p.EmpresaId
                  AND ph.ProcessoId = p.Id
            ) n
        ) hist
        WHERE p.EmpresaId = @EmpresaId
          AND (@Status IS NULL OR ps.Status = @Status)
        ORDER BY p.Id DESC
        OFFSET (@Page - 1) * @PageSize ROWS
        FETCH NEXT @PageSize ROWS ONLY;";

        var rows = await conn.QueryAsync<ProcessoListItemRow>(itensSql, new
        {
            EmpresaId = empresaId,
            Status = status,
            Page = page < 1 ? 1 : page,
            PageSize = pageSize < 1 ? 10 : pageSize
        });

        var items = rows.Select(row =>
        {
            var usuarios = string.IsNullOrWhiteSpace(row.UsuariosConcatenados)
                ? new List<string>()
                : row.UsuariosConcatenados
                    .Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(u => u.Trim())
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(u => u, StringComparer.OrdinalIgnoreCase)
                    .ToList();

            return new ProcessoListItemDto(
                row.Id,
                row.Titulo,
                row.Status,
                row.PorcentagemProgresso,
                row.CriadoEm,
                usuarios);
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

        var campoModeloIds = rows
            .Where(r => r.CampoModeloId.HasValue)
            .Select(r => r.CampoModeloId!.Value)
            .Distinct()
            .ToArray();

        var opcoesPorCampo = new Dictionary<int, List<CampoOpcaoDto>>();
        if (campoModeloIds.Length > 0)
        {
            var opcoes = (await cn.QueryAsync<CampoOpcaoDto>(@"
                SELECT Id, CampoModeloId, Texto, Valor, Ordem, Ativo
                FROM CampoConfiguracoes
                WHERE EmpresaId = @empresaId
                  AND CampoModeloId IN @ids
                ORDER BY CampoModeloId, Ordem;",
                new { empresaId, ids = campoModeloIds })).ToList();

            opcoesPorCampo = opcoes
                .GroupBy(o => o.CampoModeloId)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .Where(o => o.Ativo)
                        .OrderBy(o => o.Ordem)
                        .ToList());
        }

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
                                    c.ValorBool,
                                    opcoesPorCampo.TryGetValue(c.CampoModeloId ?? 0, out var opcoes)
                                        ? opcoes.ToList()
                                        : new List<CampoOpcaoDto>()
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

    public async Task<(int empresaId, int processoId)?> ObterEmpresaEProcessoDaFaseAsync(int faseInstanciaId)
    {
        using var cn = await _db.AbrirConexaoAsync();
        const string sql = @"SELECT EmpresaId, ProcessoId FROM FaseInstancias WHERE Id = @faseInstanciaId;";
        var row = await cn.QueryFirstOrDefaultAsync<EmpresaProcessoRow>(sql, new { faseInstanciaId });
        return row is null ? ((int, int)?)null : (row.EmpresaId, row.ProcessoId);
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
            if (!statusId.HasValue)
            {
                throw new InvalidOperationException($"Status '{status}' não encontrado para a empresa informada.");
            }
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
        var linhas = await cn.ExecuteAsync(sql, parameters);
        if (linhas == 0)
        {
            throw new InvalidOperationException("Processo não encontrado ou não atualizado.");
        }
    }

    public async Task RegistrarHistoricoAsync(int empresaId, int processoId, int? usuarioId, string usuarioNome, string descricao)
    {
        using var cn = await _db.AbrirConexaoAsync();
        const string sql = @"
            INSERT INTO ProcessoHistoricos (EmpresaId, ProcessoId, UsuarioId, UsuarioNome, Descricao)
            VALUES (@empresaId, @processoId, @usuarioId, @usuarioNome, @descricao);";

        await cn.ExecuteAsync(sql, new
        {
            empresaId,
            processoId,
            usuarioId,
            usuarioNome = string.IsNullOrWhiteSpace(usuarioNome) ? "Sistema" : usuarioNome.Trim(),
            descricao
        });
    }

    public async Task<IEnumerable<ProcessoHistoricoDto>> ListarHistoricosAsync(int empresaId, int processoId)
    {
        using var cn = await _db.AbrirConexaoAsync();
        const string sql = @"
            SELECT Id, ProcessoId, UsuarioId, UsuarioNome, Descricao, CriadoEm
            FROM ProcessoHistoricos
            WHERE EmpresaId = @empresaId AND ProcessoId = @processoId
            ORDER BY CriadoEm DESC, Id DESC;";

        return await cn.QueryAsync<ProcessoHistoricoDto>(sql, new { empresaId, processoId });
    }

    private class ProcessoListItemRow
    {
        public int Id { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int PorcentagemProgresso { get; set; }

        public DateTime CriadoEm { get; set; }

        public string? UsuariosConcatenados { get; set; }
    }

    private class EmpresaProcessoRow
    {
        public int EmpresaId { get; set; }

        public int ProcessoId { get; set; }
    }

    private class TemplateRow
    {
        public int FaseModeloId { get; set; }

        public string FaseTitulo { get; set; } = string.Empty;

        public int FaseOrdem { get; set; }

        public int? PaginaModeloId { get; set; }

        public string? PaginaTitulo { get; set; }

        public int? PaginaOrdem { get; set; }

        public int? CampoModeloId { get; set; }

        public string? CampoNome { get; set; }

        public string? CampoRotulo { get; set; }

        public string? CampoTipo { get; set; }

        public bool? CampoObrigatorio { get; set; }

        public int? CampoOrdem { get; set; }

        public string? CampoPlaceholder { get; set; }

        public string? CampoMascara { get; set; }

        public string? CampoAjuda { get; set; }
    }

    private class ProcessoGraphRow
    {
        public int FaseId { get; set; }

        public int FaseModeloId { get; set; }

        public string FaseTitulo { get; set; } = string.Empty;

        public int FaseOrdem { get; set; }

        public string FaseStatus { get; set; } = string.Empty;

        public int FaseProgresso { get; set; }

        public int? PaginaId { get; set; }

        public int? PaginaModeloId { get; set; }

        public string? PaginaTitulo { get; set; }

        public int? PaginaOrdem { get; set; }

        public bool? PaginaConcluida { get; set; }

        public int? CampoId { get; set; }

        public int? CampoModeloId { get; set; }

        public string? CampoNome { get; set; }

        public string? CampoRotulo { get; set; }

        public string? CampoTipo { get; set; }

        public bool? CampoObrigatorio { get; set; }

        public int? CampoOrdem { get; set; }

        public string? CampoPlaceholder { get; set; }

        public string? CampoMascara { get; set; }

        public string? CampoAjuda { get; set; }

        public string? ValorTexto { get; set; }

        public decimal? ValorNumero { get; set; }

        public DateTime? ValorData { get; set; }

        public bool? ValorBool { get; set; }
    }
}
