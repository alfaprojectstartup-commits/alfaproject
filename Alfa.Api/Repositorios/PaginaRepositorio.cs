using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Alfa.Api.Db;
using Alfa.Api.Dtos;
using Alfa.Api.Repositorios.Interfaces;
using Alfa.Api.Dados;

namespace Alfa.Api.Repositorios
{
    public class PaginaRepositorio : IPaginaRepositorio
    {
        private readonly IConexaoSql _db;
        public PaginaRepositorio(IConexaoSql db) => _db = db;

        public async Task<IEnumerable<PaginaTemplateDto>> ListarTemplatesPorFaseTemplateAsync(
            int empresaId, int faseTemplateId)
        {
            using IDbConnection conn = await _db.AbrirAsync();
            const string sql = @"
                SELECT Id, FaseTemplateId, Titulo, Ordem
                FROM PaginaTemplate           -- << ajuste se necessário
                WHERE EmpresaId = @EmpresaId
                  AND FaseTemplateId = @FaseTemplateId
                ORDER BY Ordem;";

            return await conn.QueryAsync<PaginaTemplateDto>(sql, new
            {
                EmpresaId = empresaId,
                FaseTemplateId = faseTemplateId
            });
        }

        public async Task<IEnumerable<PaginaTemplateDto>> ListarTemplatesPorFaseInstanceAsync(
            int empresaId, int faseInstanceId)
        {
            using IDbConnection conn = await _db.AbrirAsync();
            const string sql = @"
                SELECT p.Id, p.FaseTemplateId, p.Titulo, p.Ordem
                FROM PaginaTemplate p        -- << ajuste se necessário
                JOIN FaseInstance fi         -- << assume fi tem FaseTemplateId
                  ON fi.EmpresaId = p.EmpresaId
                 AND fi.FaseTemplateId = p.FaseTemplateId
                WHERE p.EmpresaId = @EmpresaId
                  AND fi.Id        = @FaseInstanceId
                ORDER BY p.Ordem;";

            return await conn.QueryAsync<PaginaTemplateDto>(sql, new
            {
                EmpresaId = empresaId,
                FaseInstanceId = faseInstanceId
            });
        }
    }
}
