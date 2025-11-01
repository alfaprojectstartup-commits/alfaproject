using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Alfa.Api.Dtos;
using Alfa.Api.Repositorios.Interfaces;
using Alfa.Api.Infra.Interfaces;

namespace Alfa.Api.Repositorios
{
    public class PaginaRepositorio : IPaginaRepositorio
    {
        private readonly IConexaoSql _db;
        public PaginaRepositorio(IConexaoSql db) => _db = db;

        public async Task<IEnumerable<PaginaModeloDto>> ListarTemplatesPorFaseModeloAsync(
            int empresaId, int faseModeloId)
        {
            using var conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT Id, FaseModeloId, Titulo, Ordem
                FROM PaginaModelos
                WHERE EmpresaId = @EmpresaId
                  AND FaseModeloId = @FaseModeloId
                ORDER BY Ordem;";

            return await conn.QueryAsync<PaginaModeloDto>(sql, new
            {
                EmpresaId = empresaId,
                FaseModeloId = faseModeloId
            });
        }

        public async Task<IEnumerable<PaginaModeloDto>> ListarTemplatesPorFaseInstanciaAsync(
            int empresaId, int faseInstanciaId)
        {
            using var conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT pm.Id, pm.FaseModeloId, pm.Titulo, pm.Ordem
                FROM PaginaInstancias pi
                JOIN PaginaModelos pm
                  ON pm.EmpresaId = pi.EmpresaId
                 AND pm.Id = pi.PaginaModeloId
                WHERE pi.EmpresaId = @EmpresaId
                  AND pi.FaseInstanciaId = @FaseInstanciaId
                ORDER BY pm.Ordem;";

            return await conn.QueryAsync<PaginaModeloDto>(sql, new
            {
                EmpresaId = empresaId,
                FaseInstanciaId = faseInstanciaId
            });
        }
    }
}
