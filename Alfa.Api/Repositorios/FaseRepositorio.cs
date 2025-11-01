using System;
using System.Collections.Generic;
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

        private record FaseInstanciaRow(
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
            System.DateTime? ValorData,
            bool? ValorBool);
    }
}
