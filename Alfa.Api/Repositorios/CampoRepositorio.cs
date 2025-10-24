using System.Data;
using Dapper;
using Alfa.Api.Db;
using Alfa.Api.Dtos;
using Alfa.Api.Repositorios.Interfaces;
using Alfa.Api.Infra.Interfaces;

namespace Alfa.Api.Repositorios
{
    public class CampoRepositorio : ICampoRepositorio
    {
        private readonly IConexaoSql _db;
        public CampoRepositorio(IConexaoSql db) => _db = db;

        public async Task<IEnumerable<CampoModeloDto>> ListarPorPaginaAsync(
            int empresaId, int PaginaModelosId)
        {
            using IDbConnection conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT Id, PaginaModelosId, Tipo, Rotulo, Obrigatorio, Ordem,
                       Placeholder, Mascara, Ajuda
                FROM CampoModelo                -- << ajuste se necessário
                WHERE EmpresaId = @EmpresaId
                  AND PaginaModelosId = @PaginaModelosId
                ORDER BY Ordem;";

            return await conn.QueryAsync<CampoModeloDto>(sql, new
            {
                EmpresaId = empresaId,
                PaginaModelosId = PaginaModelosId
            });
        }

        public async Task<IEnumerable<CampoOpcaoDto>> ListarOpcoesAsync(
            int empresaId, int fieldTemplateId)
        {
            using IDbConnection conn = await  _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT Id, CampoModeloId, Valor, Texto, Ordem
                FROM CampoOpcao                   -- << ajuste se necessário
                WHERE EmpresaId = @EmpresaId
                  AND CampoModeloId = @CampoModeloId
                ORDER BY Ordem;";

            return await conn.QueryAsync<CampoOpcaoDto>(sql, new
            {
                EmpresaId = empresaId,
                CampoModeloId = fieldTemplateId
            });
        }
    }
}
