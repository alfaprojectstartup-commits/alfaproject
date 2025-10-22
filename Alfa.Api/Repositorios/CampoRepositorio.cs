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
    public class CampoRepositorio : ICampoRepositorio
    {
        private readonly IConexaoSql _db;
        public CampoRepositorio(IConexaoSql db) => _db = db;

        public async Task<IEnumerable<CampoTemplateDto>> ListarPorPaginaAsync(
            int empresaId, int paginaTemplateId)
        {
            using IDbConnection conn = await _db.AbrirAsync();
            const string sql = @"
                SELECT Id, PaginaTemplateId, Tipo, Rotulo, Obrigatorio, Ordem,
                       Placeholder, Mascara, Ajuda
                FROM CampoTemplate                -- << ajuste se necessário
                WHERE EmpresaId = @EmpresaId
                  AND PaginaTemplateId = @PaginaTemplateId
                ORDER BY Ordem;";

            return await conn.QueryAsync<CampoTemplateDto>(sql, new
            {
                EmpresaId = empresaId,
                PaginaTemplateId = paginaTemplateId
            });
        }

        public async Task<IEnumerable<CampoOpcaoDto>> ListarOpcoesAsync(
            int empresaId, int fieldTemplateId)
        {
            using IDbConnection conn = await  _db.AbrirAsync();
            const string sql = @"
                SELECT Id, CampoTemplateId, Valor, Texto, Ordem
                FROM CampoOpcao                   -- << ajuste se necessário
                WHERE EmpresaId = @EmpresaId
                  AND CampoTemplateId = @CampoTemplateId
                ORDER BY Ordem;";

            return await conn.QueryAsync<CampoOpcaoDto>(sql, new
            {
                EmpresaId = empresaId,
                CampoTemplateId = fieldTemplateId
            });
        }
    }
}
