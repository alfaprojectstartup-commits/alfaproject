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
    public class RespostaRepositorio : IRespostaRepositorio
    {
        private readonly IConexaoSql _db;
        public RespostaRepositorio(IConexaoSql db) => _db = db;

        public async Task SalvarPaginaAsync(int empresaId, PaginaRespostaDto dto)
        {
            using IDbConnection conn = await _db.AbrirConexaoAsync();
            using var tx = conn.BeginTransaction();

            const string validarSql = @"
                SELECT COUNT(1)
                FROM PaginaInstancias
                WHERE EmpresaId = @EmpresaId
                  AND Id = @PaginaInstanciaId
                  AND FaseInstanciaId = @FaseInstanciaId;";

            var valido = await conn.ExecuteScalarAsync<int>(validarSql, new
            {
                EmpresaId = empresaId,
                PaginaInstanciaId = dto.PaginaInstanciaId,
                FaseInstanciaId = dto.FaseInstanciaId
            }, tx);

            if (valido == 0)
            {
                tx.Rollback();
                throw new KeyNotFoundException("Página não pertence à fase informada ou empresa inválida.");
            }

            var rows = (dto.Campos ?? new List<FieldResponseDto>())
                .Select(f => new
                {
                    EmpresaId = empresaId,
                    PaginaInstanciaId = dto.PaginaInstanciaId,
                    CampoInstanciaId = f.CampoInstanciaId,
                    ValorTexto = f.ValorTexto?.Trim(),
                    ValorNumero = f.ValorNumero,
                    ValorData = f.ValorData,
                    ValorBool = f.ValorBool
                })
                .ToList();

            const string updateSql = @"
                UPDATE CampoInstancias
                SET ValorTexto = @ValorTexto,
                    ValorNumero = @ValorNumero,
                    ValorData = @ValorData,
                    ValorBool = @ValorBool
                WHERE EmpresaId = @EmpresaId
                  AND PaginaInstanciaId = @PaginaInstanciaId
                  AND Id = @CampoInstanciaId;";

            foreach (var row in rows)
            {
                await conn.ExecuteAsync(updateSql, row, tx);
            }

            const string resumoSql = @"
                SELECT
                    SUM(CASE WHEN Obrigatorio = 1 THEN 1 ELSE 0 END) AS Obrigatorios,
                    SUM(CASE WHEN Obrigatorio = 1 AND (
                            NULLIF(LTRIM(RTRIM(ISNULL(ValorTexto, ''))), '') IS NOT NULL
                            OR ValorNumero IS NOT NULL
                            OR ValorData IS NOT NULL
                            OR ValorBool IS NOT NULL
                        ) THEN 1 ELSE 0 END) AS Preenchidos
                FROM CampoInstancias
                WHERE EmpresaId = @EmpresaId
                  AND PaginaInstanciaId = @PaginaInstanciaId;";

            var resumo = await conn.QuerySingleAsync<(int obrig, int preenc)>(resumoSql, new
            {
                EmpresaId = empresaId,
                PaginaInstanciaId = dto.PaginaInstanciaId
            }, tx);

            var concluida = resumo.obrig == 0 || resumo.obrig == resumo.preenc;
            const string atualizarPaginaSql = @"
                UPDATE PaginaInstancias
                SET Concluida = @Concluida
                WHERE EmpresaId = @EmpresaId
                  AND Id = @PaginaInstanciaId;";

            await conn.ExecuteAsync(atualizarPaginaSql, new
            {
                EmpresaId = empresaId,
                PaginaInstanciaId = dto.PaginaInstanciaId,
                Concluida = concluida ? 1 : 0
            }, tx);

            tx.Commit();
        }

        public async Task<int> ContarCamposObrigatoriosAsync(int empresaId, int paginaInstanciaId)
        {
            using IDbConnection conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT COUNT(1)
                FROM CampoInstancias
                WHERE EmpresaId = @EmpresaId
                  AND PaginaInstanciaId = @PaginaInstanciaId
                  AND Obrigatorio = 1;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                EmpresaId = empresaId,
                PaginaInstanciaId = paginaInstanciaId
            });
        }

        public async Task<int> ContarCamposPreenchidosAsync(int empresaId, int paginaInstanciaId)
        {
            using IDbConnection conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT COUNT(1)
                FROM CampoInstancias
                WHERE EmpresaId = @EmpresaId
                  AND PaginaInstanciaId = @PaginaInstanciaId
                  AND Obrigatorio = 1
                  AND (
                        NULLIF(LTRIM(RTRIM(ISNULL(ValorTexto, ''))), '') IS NOT NULL
                        OR ValorNumero IS NOT NULL
                        OR ValorData IS NOT NULL
                        OR ValorBool IS NOT NULL
                  );";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                EmpresaId = empresaId,
                PaginaInstanciaId = paginaInstanciaId
            });
        }

        public async Task<int> ContarPaginasDaFaseAsync(int empresaId, int faseInstanciaId)
        {
            using IDbConnection conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT COUNT(1)
                FROM PaginaInstancias
                WHERE EmpresaId = @EmpresaId
                  AND FaseInstanciaId = @FaseInstanciaId;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                EmpresaId = empresaId,
                FaseInstanciaId = faseInstanciaId
            });
        }

        public async Task<IEnumerable<(int paginaInstanciaId, int obrig, int preenc)>> ObterResumoPorPaginasDaFaseAsync(
            int empresaId, int faseInstanciaId)
        {
            using IDbConnection conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT
                    pi.Id AS PaginaInstanciaId,
                    SUM(CASE WHEN ci.Obrigatorio = 1 THEN 1 ELSE 0 END) AS Obrigatorios,
                    SUM(CASE WHEN ci.Obrigatorio = 1 AND (
                            NULLIF(LTRIM(RTRIM(ISNULL(ci.ValorTexto, ''))), '') IS NOT NULL
                            OR ci.ValorNumero IS NOT NULL
                            OR ci.ValorData IS NOT NULL
                            OR ci.ValorBool IS NOT NULL
                        ) THEN 1 ELSE 0 END) AS Preenchidos
                FROM PaginaInstancias pi
                LEFT JOIN CampoInstancias ci
                  ON ci.EmpresaId = pi.EmpresaId
                 AND ci.PaginaInstanciaId = pi.Id
                WHERE pi.EmpresaId = @EmpresaId
                  AND pi.FaseInstanciaId = @FaseInstanciaId
                GROUP BY pi.Id
                ORDER BY MAX(pi.Ordem);";

            return await conn.QueryAsync<(int paginaInstanciaId, int obrig, int preenc)>(sql, new
            {
                EmpresaId = empresaId,
                FaseInstanciaId = faseInstanciaId
            });
        }
    }
}
