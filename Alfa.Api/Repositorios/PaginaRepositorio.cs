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

        public async Task<IEnumerable<PaginaModelosDto>> ListarTemplatesPorFaseModelosAsync(
            int empresaId, int FaseModeloId)
        {
            using IDbConnection conn = await _db.AbrirAsync();
            const string sql = @"
                SELECT Id, FaseModeloId, Titulo, Ordem
                FROM PaginaModelos           -- << ajuste se necessário
                WHERE EmpresaId = @EmpresaId
                  AND FaseModeloId = @FaseModeloId
                ORDER BY Ordem;";

            return await conn.QueryAsync<PaginaModelosDto>(sql, new
            {
                EmpresaId = empresaId,
                FaseModeloId = FaseModeloId
            });
        }

        public async Task<IEnumerable<PaginaModelosDto>> ListarTemplatesPorFasesAsync(
            int empresaId, int FasesId)
        {
            using IDbConnection conn = await _db.AbrirAsync();
            const string sql = @"
                SELECT p.Id, p.FaseModeloId, p.Titulo, p.Ordem
                FROM PaginaModelos p        -- << ajuste se necessário
                JOIN Fases fi         -- << assume fi tem FaseModeloId
                  ON fi.EmpresaId = p.EmpresaId
                 AND fi.FaseModeloId = p.FaseModeloId
                WHERE p.EmpresaId = @EmpresaId
                  AND fi.Id        = @FasesId
                ORDER BY p.Ordem;";

            return await conn.QueryAsync<PaginaModelosDto>(sql, new
            {
                EmpresaId = empresaId,
                FasesId = FasesId
            });
        }
    }
}
