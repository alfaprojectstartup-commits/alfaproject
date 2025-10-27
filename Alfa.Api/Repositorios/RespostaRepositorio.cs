using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Alfa.Api.Db;
using Alfa.Api.Dtos;
using Alfa.Api.Repositorios.Interfaces;
using System.Globalization;
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

            const string delSql = @"
        DELETE FROM RespostaCampo
        WHERE EmpresaId = @EmpresaId
          AND FasesId = @FasesId
          AND PaginaModelosId = @PaginaModelosId;";

            await conn.ExecuteAsync(delSql, new
            {
                EmpresaId = empresaId,
                FasesId = dto.FasesId,
                PaginaModelosId = dto.PaginaModelosId
            }, tx);

            // converte valores tipados para string única "Valor"
            string? ToValor(FieldResponseDto f)
            {
                if (!string.IsNullOrWhiteSpace(f.ValorTexto)) return f.ValorTexto?.Trim();
                if (f.ValorNumero.HasValue) return f.ValorNumero.Value.ToString(CultureInfo.InvariantCulture);
                if (f.ValorData.HasValue) return f.ValorData.Value.ToString("yyyy-MM-dd");
                if (f.ValorBool.HasValue) return f.ValorBool.Value ? "true" : "false";
                return null;
            }

            var rows = (dto.Campos ?? new List<FieldResponseDto>())
                .Select(f => new
                {
                    EmpresaId = empresaId,
                    FasesId = dto.FasesId,
                    PaginaModelosId = dto.PaginaModelosId,
                    CampoModeloId = f.FieldTemplateId,
                    Valor = ToValor(f)
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Valor))
                .ToList();

            if (rows.Count > 0)
            {
                const string insSql = @"
            INSERT INTO RespostaCampo
                (EmpresaId, FasesId, PaginaModelosId, CampoModeloId, Valor)
            VALUES
                (@EmpresaId, @FasesId, @PaginaModelosId, @CampoModeloId, @Valor);";

                await conn.ExecuteAsync(insSql, rows, tx);
            }

            tx.Commit();
        }

        // Quantos campos obrigatórios existem em uma página
        public async Task<int> ContarCamposObrigatoriosAsync(int empresaId, int PaginaModelosId)
        {
            using IDbConnection conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT COUNT(1)
                FROM CampoModelo
                WHERE EmpresaId = @EmpresaId
                  AND PaginaModelosId = @PaginaModelosId
                  AND Obrigatorio = 1;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                EmpresaId = empresaId,
                PaginaModelosId = PaginaModelosId
            });
        }

        // Quantos desses obrigatórios já têm resposta não-vazia na fase
        public async Task<int> ContarCamposPreenchidosAsync(int empresaId, int FasesId, int PaginaModelosId)
        {
            using IDbConnection conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT COUNT(1)
                FROM RespostaCampo r
                JOIN CampoModelo c
                  ON c.EmpresaId = r.EmpresaId
                 AND c.Id        = r.CampoModeloId
                WHERE r.EmpresaId = @EmpresaId
                  AND r.FasesId = @FasesId
                  AND r.PaginaModelosId = @PaginaModelosId
                  AND c.Obrigatorio = 1
                  AND NULLIF(LTRIM(RTRIM(r.Valor)), '') IS NOT NULL;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                EmpresaId = empresaId,
                FasesId = FasesId,
                PaginaModelosId = PaginaModelosId
            });
        }

        // Quantidade total de páginas (templates) pertencentes à fase
        public async Task<int> ContarPaginasDaFaseAsync(int empresaId, int FasesId)
        {
            using IDbConnection conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT COUNT(1)
                FROM PaginaModelos p
                JOIN Fases fi
                  ON fi.EmpresaId = p.EmpresaId
                 AND fi.FaseModeloId = p.FaseModeloId
                WHERE p.EmpresaId = @EmpresaId
                  AND fi.Id = @FasesId;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                EmpresaId = empresaId,
                FasesId = FasesId
            });
        }

        // Retorna para cada página (da fase) o trio: paginaId, obrigatórios e preenchidos
        public async Task<IEnumerable<(int paginaId, int obrig, int preenc)>> ObterResumoPorPaginasDaFaseAsync(
            int empresaId, int FasesId)
        {
            using IDbConnection conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                ;WITH P AS (
                    SELECT p.Id AS PaginaId
                    FROM PaginaModelos p
                    JOIN Fases fi
                      ON fi.EmpresaId = p.EmpresaId
                     AND fi.FaseModeloId = p.FaseModeloId
                    WHERE p.EmpresaId = @EmpresaId
                      AND fi.Id = @FasesId
                ),
                OBR AS (
                    SELECT c.PaginaModelosId AS PaginaId, COUNT(1) AS Obrig
                    FROM CampoModelo c
                    JOIN P ON P.PaginaId = c.PaginaModelosId
                    WHERE c.EmpresaId = @EmpresaId AND c.Obrigatorio = 1
                    GROUP BY c.PaginaModelosId
                ),
                PRE AS (
                    SELECT r.PaginaModelosId AS PaginaId, COUNT(1) AS Preenc
                    FROM RespostaCampo r
                    JOIN CampoModelo c
                      ON c.EmpresaId = r.EmpresaId
                     AND c.Id        = r.CampoModeloId
                    WHERE r.EmpresaId = @EmpresaId
                      AND r.FasesId = @FasesId
                      AND c.Obrigatorio = 1
                      AND NULLIF(LTRIM(RTRIM(r.Valor)), '') IS NOT NULL
                    GROUP BY r.PaginaModelosId
                )
                SELECT P.PaginaId AS paginaId,
                       ISNULL(OBR.Obrig, 0) AS obrig,
                       ISNULL(PRE.Preenc, 0) AS preenc
                FROM P
                LEFT JOIN OBR ON OBR.PaginaId = P.PaginaId
                LEFT JOIN PRE ON PRE.PaginaId = P.PaginaId
                ORDER BY P.PaginaId;";

            return await conn.QueryAsync<(int paginaId, int obrig, int preenc)>(sql, new
            {
                EmpresaId = empresaId,
                FasesId = FasesId
            });
        }
    }
}
