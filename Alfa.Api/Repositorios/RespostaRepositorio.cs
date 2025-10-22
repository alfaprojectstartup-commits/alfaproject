using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Alfa.Api.Db;
using Alfa.Api.Dtos;
using Alfa.Api.Repositorios.Interfaces;
using Alfa.Api.Dados;
using System.Globalization;

namespace Alfa.Api.Repositorios
{
    public class RespostaRepositorio : IRespostaRepositorio
    {
        private readonly IConexaoSql _db;
        public RespostaRepositorio(IConexaoSql db) => _db = db;

        public async Task SalvarPaginaAsync(int empresaId, PageResponseDto dto)
        {
            using IDbConnection conn = await _db.AbrirAsync();
            using var tx = conn.BeginTransaction();

            const string delSql = @"
        DELETE FROM RespostaCampo
        WHERE EmpresaId = @EmpresaId
          AND FaseInstanceId = @FaseInstanceId
          AND PaginaTemplateId = @PaginaTemplateId;";

            await conn.ExecuteAsync(delSql, new
            {
                EmpresaId = empresaId,
                FaseInstanceId = dto.FaseInstanceId,
                PaginaTemplateId = dto.PaginaTemplateId
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
                    FaseInstanceId = dto.FaseInstanceId,
                    PaginaTemplateId = dto.PaginaTemplateId,
                    CampoTemplateId = f.FieldTemplateId,
                    Valor = ToValor(f)
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Valor))
                .ToList();

            if (rows.Count > 0)
            {
                const string insSql = @"
            INSERT INTO RespostaCampo
                (EmpresaId, FaseInstanceId, PaginaTemplateId, CampoTemplateId, Valor)
            VALUES
                (@EmpresaId, @FaseInstanceId, @PaginaTemplateId, @CampoTemplateId, @Valor);";

                await conn.ExecuteAsync(insSql, rows, tx);
            }

            tx.Commit();
        }

        // Quantos campos obrigatórios existem em uma página
        public async Task<int> ContarCamposObrigatoriosAsync(int empresaId, int paginaTemplateId)
        {
            using IDbConnection conn = await _db.AbrirAsync();
            const string sql = @"
                SELECT COUNT(1)
                FROM CampoTemplate
                WHERE EmpresaId = @EmpresaId
                  AND PaginaTemplateId = @PaginaTemplateId
                  AND Obrigatorio = 1;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                EmpresaId = empresaId,
                PaginaTemplateId = paginaTemplateId
            });
        }

        // Quantos desses obrigatórios já têm resposta não-vazia na fase
        public async Task<int> ContarCamposPreenchidosAsync(int empresaId, int faseInstanceId, int paginaTemplateId)
        {
            using IDbConnection conn = await _db.AbrirAsync();
            const string sql = @"
                SELECT COUNT(1)
                FROM RespostaCampo r
                JOIN CampoTemplate c
                  ON c.EmpresaId = r.EmpresaId
                 AND c.Id        = r.CampoTemplateId
                WHERE r.EmpresaId = @EmpresaId
                  AND r.FaseInstanceId = @FaseInstanceId
                  AND r.PaginaTemplateId = @PaginaTemplateId
                  AND c.Obrigatorio = 1
                  AND NULLIF(LTRIM(RTRIM(r.Valor)), '') IS NOT NULL;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                EmpresaId = empresaId,
                FaseInstanceId = faseInstanceId,
                PaginaTemplateId = paginaTemplateId
            });
        }

        // Quantidade total de páginas (templates) pertencentes à fase
        public async Task<int> ContarPaginasDaFaseAsync(int empresaId, int faseInstanceId)
        {
            using IDbConnection conn = await _db.AbrirAsync();
            const string sql = @"
                SELECT COUNT(1)
                FROM PaginaTemplate p
                JOIN FaseInstance fi
                  ON fi.EmpresaId = p.EmpresaId
                 AND fi.FaseTemplateId = p.FaseTemplateId
                WHERE p.EmpresaId = @EmpresaId
                  AND fi.Id = @FaseInstanceId;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                EmpresaId = empresaId,
                FaseInstanceId = faseInstanceId
            });
        }

        // Retorna para cada página (da fase) o trio: paginaId, obrigatórios e preenchidos
        public async Task<IEnumerable<(int paginaId, int obrig, int preenc)>> ObterResumoPorPaginasDaFaseAsync(
            int empresaId, int faseInstanceId)
        {
            using IDbConnection conn = await _db.AbrirAsync();
            const string sql = @"
                ;WITH P AS (
                    SELECT p.Id AS PaginaId
                    FROM PaginaTemplate p
                    JOIN FaseInstance fi
                      ON fi.EmpresaId = p.EmpresaId
                     AND fi.FaseTemplateId = p.FaseTemplateId
                    WHERE p.EmpresaId = @EmpresaId
                      AND fi.Id = @FaseInstanceId
                ),
                OBR AS (
                    SELECT c.PaginaTemplateId AS PaginaId, COUNT(1) AS Obrig
                    FROM CampoTemplate c
                    JOIN P ON P.PaginaId = c.PaginaTemplateId
                    WHERE c.EmpresaId = @EmpresaId AND c.Obrigatorio = 1
                    GROUP BY c.PaginaTemplateId
                ),
                PRE AS (
                    SELECT r.PaginaTemplateId AS PaginaId, COUNT(1) AS Preenc
                    FROM RespostaCampo r
                    JOIN CampoTemplate c
                      ON c.EmpresaId = r.EmpresaId
                     AND c.Id        = r.CampoTemplateId
                    WHERE r.EmpresaId = @EmpresaId
                      AND r.FaseInstanceId = @FaseInstanceId
                      AND c.Obrigatorio = 1
                      AND NULLIF(LTRIM(RTRIM(r.Valor)), '') IS NOT NULL
                    GROUP BY r.PaginaTemplateId
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
                FaseInstanceId = faseInstanceId
            });
        }
    }
}
