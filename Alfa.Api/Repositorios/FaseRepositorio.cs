using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Alfa.Api.Dtos;
using Alfa.Api.Repositorios.Interfaces;
using Alfa.Api.Infra.Interfaces;

namespace Alfa.Api.Repositorios
{
    public class FaseRepositorio : IFaseRepositorio
    {
        private readonly IConexaoSql _db;
        private readonly IRespostaRepositorio _respostas;

        public FaseRepositorio(IConexaoSql db, IRespostaRepositorio respostas)
        {
            _db = db;
            _respostas = respostas;
        }

        public async Task<IEnumerable<FaseModeloDto>> ListarTemplatesAsync(int empresaId)
        {
            using var conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT Id, Titulo, Ordem, Ativo
                FROM FaseModelos
                WHERE EmpresaId = @EmpresaId
                ORDER BY Ordem;";

            return await conn.QueryAsync<FaseModeloDto>(sql, new { EmpresaId = empresaId });
        }

        public async Task<FaseModeloDto?> ObterTemplateAsync(int empresaId, int faseModeloId)
        {
            using var conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT Id, Titulo, Ordem, Ativo
                FROM FaseModelos
                WHERE EmpresaId = @EmpresaId
                  AND Id = @FaseModeloId;";

            return await conn.QueryFirstOrDefaultAsync<FaseModeloDto>(sql, new
            {
                EmpresaId = empresaId,
                FaseModeloId = faseModeloId
            });
        }

        public async Task<int> CriarTemplateAsync(int empresaId, FaseModeloInputDto dto)
        {
            using var conn = await _db.AbrirConexaoAsync();
            using var tx = conn.BeginTransaction();

            var faseId = await conn.ExecuteScalarAsync<int>(
                @"INSERT INTO FaseModelos (EmpresaId, Titulo, Ordem, Ativo)
                  VALUES (@EmpresaId, @Titulo, @Ordem, @Ativo);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new
                {
                    EmpresaId = empresaId,
                    Titulo = dto.Titulo?.Trim() ?? string.Empty,
                    dto.Ordem,
                    dto.Ativo
                }, tx);

            await InserirEstruturaAsync(conn, tx, empresaId, faseId, dto.Paginas);

            tx.Commit();
            return faseId;
        }

        public async Task AtualizarTemplateAsync(int empresaId, int faseModeloId, FaseModeloInputDto dto)
        {
            using var conn = await _db.AbrirConexaoAsync();
            using var tx = conn.BeginTransaction();

            var linhas = await conn.ExecuteAsync(
                @"UPDATE FaseModelos
                     SET Titulo = @Titulo,
                         Ordem = @Ordem,
                         Ativo = @Ativo
                   WHERE EmpresaId = @EmpresaId
                     AND Id = @FaseModeloId;",
                new
                {
                    EmpresaId = empresaId,
                    FaseModeloId = faseModeloId,
                    Titulo = dto.Titulo?.Trim() ?? string.Empty,
                    dto.Ordem,
                    dto.Ativo
                }, tx);

            if (linhas == 0)
            {
                tx.Rollback();
                throw new KeyNotFoundException("Fase não encontrada para atualização.");
            }

            var paginaIds = (await conn.QueryAsync<int>(
                @"SELECT Id
                    FROM PaginaModelos
                   WHERE EmpresaId = @EmpresaId
                     AND FaseModeloId = @FaseModeloId;",
                new
                {
                    EmpresaId = empresaId,
                    FaseModeloId = faseModeloId
                }, tx)).ToList();

            if (paginaIds.Count > 0)
            {
                var campoIds = (await conn.QueryAsync<int>(
                    @"SELECT Id
                        FROM CampoModelos
                       WHERE EmpresaId = @EmpresaId
                         AND PaginaModeloId IN @PaginaIds;",
                    new
                    {
                        EmpresaId = empresaId,
                        PaginaIds = paginaIds
                    }, tx)).ToList();

                if (campoIds.Count > 0)
                {
                    await conn.ExecuteAsync(
                        @"DELETE FROM CampoConfiguracoes
                          WHERE EmpresaId = @EmpresaId
                            AND CampoModeloId IN @CampoIds;",
                        new
                        {
                            EmpresaId = empresaId,
                            CampoIds = campoIds
                        }, tx);
                }

                await conn.ExecuteAsync(
                    @"DELETE FROM CampoModelos
                      WHERE EmpresaId = @EmpresaId
                        AND PaginaModeloId IN @PaginaIds;",
                    new
                    {
                        EmpresaId = empresaId,
                        PaginaIds = paginaIds
                    }, tx);

                await conn.ExecuteAsync(
                    @"DELETE FROM PaginaModelos
                      WHERE EmpresaId = @EmpresaId
                        AND Id IN @PaginaIds;",
                    new
                    {
                        EmpresaId = empresaId,
                        PaginaIds = paginaIds
                    }, tx);
            }

            await InserirEstruturaAsync(conn, tx, empresaId, faseModeloId, dto.Paginas);

            tx.Commit();
        }

        public async Task<IEnumerable<FaseInstanciaDto>> ListarInstanciasAsync(int empresaId, int processoId)
        {
            using var conn = await _db.AbrirConexaoAsync();
            const string sql = @"
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
                WHERE fi.EmpresaId = @EmpresaId AND fi.ProcessoId = @ProcessoId
                ORDER BY fi.Ordem, pi.Ordem, ci.Ordem;";

            var rows = (await conn.QueryAsync<FaseInstanciaRow>(sql, new
            {
                EmpresaId = empresaId,
                ProcessoId = processoId
            })).ToList();

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
                                .ToList()));

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

            return fases;
        }

        public async Task RecalcularProgressoFaseAsync(int empresaId, int faseInstanciaId)
        {
            var resumo = (await _respostas.ObterResumoPorPaginasDaFaseAsync(empresaId, faseInstanciaId)).ToList();

            double progresso = 0;
            if (resumo.Count > 0)
            {
                var percentuais = resumo.Select(x => x.obrig == 0 ? 100.0 : (100.0 * x.preenc / x.obrig));
                progresso = percentuais.Average();
            }

            var progressoInt = (int)System.Math.Round(progresso);

            using var conn = await _db.AbrirConexaoAsync();
            const string updSql = @"
                UPDATE FaseInstancias
                SET PorcentagemProgresso = @Progresso
                WHERE EmpresaId = @EmpresaId
                  AND Id = @FaseInstanciaId;";

            await conn.ExecuteAsync(updSql, new
            {
                EmpresaId = empresaId,
                FaseInstanciaId = faseInstanciaId,
                Progresso = progressoInt
            });
        }

        private static async Task InserirEstruturaAsync(
            IDbConnection conn,
            IDbTransaction tx,
            int empresaId,
            int faseModeloId,
            IEnumerable<PaginaModeloInputDto>? paginas)
        {
            if (paginas is null)
            {
                return;
            }

            foreach (var pagina in paginas.OrderBy(p => p.Ordem))
            {
                var paginaId = await conn.ExecuteScalarAsync<int>(
                    @"INSERT INTO PaginaModelos (EmpresaId, FaseModeloId, Titulo, Ordem)
                      VALUES (@EmpresaId, @FaseModeloId, @Titulo, @Ordem);
                      SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new
                    {
                        EmpresaId = empresaId,
                        FaseModeloId = faseModeloId,
                        Titulo = pagina.Titulo?.Trim() ?? string.Empty,
                        pagina.Ordem
                    }, tx);

                if (pagina.Campos is null)
                {
                    continue;
                }

                foreach (var campo in pagina.Campos.OrderBy(c => c.Ordem))
                {
                    var campoId = await conn.ExecuteScalarAsync<int>(
                        @"INSERT INTO CampoModelos
                              (EmpresaId, PaginaModeloId, NomeCampo, Rotulo, Tipo, Obrigatorio, Ordem, Placeholder, Mascara, Ajuda)
                          VALUES
                              (@EmpresaId, @PaginaModeloId, @NomeCampo, @Rotulo, @Tipo, @Obrigatorio, @Ordem, @Placeholder, @Mascara, @Ajuda);
                          SELECT CAST(SCOPE_IDENTITY() AS INT);",
                        new
                        {
                            EmpresaId = empresaId,
                            PaginaModeloId = paginaId,
                            NomeCampo = campo.NomeCampo?.Trim() ?? string.Empty,
                            Rotulo = campo.Rotulo?.Trim() ?? string.Empty,
                            Tipo = campo.Tipo?.Trim() ?? string.Empty,
                            Obrigatorio = campo.Obrigatorio,
                            campo.Ordem,
                            Placeholder = string.IsNullOrWhiteSpace(campo.Placeholder) ? null : campo.Placeholder.Trim(),
                            Mascara = string.IsNullOrWhiteSpace(campo.Mascara) ? null : campo.Mascara.Trim(),
                            Ajuda = string.IsNullOrWhiteSpace(campo.Ajuda) ? null : campo.Ajuda.Trim()
                        }, tx);

                    if (campo.Opcoes is null || !campo.Opcoes.Any())
                    {
                        continue;
                    }

                    foreach (var opcao in campo.Opcoes.OrderBy(o => o.Ordem))
                    {
                        await conn.ExecuteAsync(
                            @"INSERT INTO CampoConfiguracoes
                                  (EmpresaId, CampoModeloId, Texto, Valor, Ordem, Ativo)
                              VALUES
                                  (@EmpresaId, @CampoModeloId, @Texto, @Valor, @Ordem, @Ativo);",
                            new
                            {
                                EmpresaId = empresaId,
                                CampoModeloId = campoId,
                                Texto = opcao.Texto?.Trim() ?? string.Empty,
                                Valor = string.IsNullOrWhiteSpace(opcao.Valor) ? null : opcao.Valor.Trim(),
                                opcao.Ordem,
                                opcao.Ativo
                            }, tx);
                    }
                }
            }
        }

        private class FaseInstanciaRow
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

            public System.DateTime? ValorData { get; set; }

            public bool? ValorBool { get; set; }
        }
    }
}
