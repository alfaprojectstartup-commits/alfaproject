using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Alfa.Api.Dtos;
using Alfa.Api.Repositorios.Interfaces;
using Alfa.Api.Infra.Interfaces;

namespace Alfa.Api.Repositorios
{
    public class CampoRepositorio : ICampoRepositorio
    {
        private readonly IConexaoSql _db;
        public CampoRepositorio(IConexaoSql db) => _db = db;

        public async Task<IEnumerable<CampoModeloDto>> ListarPorPaginaModeloAsync(
            int empresaId, int paginaModeloId)
        {
            using var conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT Id, PaginaModeloId, NomeCampo, Rotulo, Tipo, Obrigatorio, Ordem, Placeholder, Mascara, Ajuda
                FROM CampoModelos
                WHERE EmpresaId = @EmpresaId
                  AND PaginaModeloId = @PaginaModeloId
                ORDER BY Ordem;";

            return await conn.QueryAsync<CampoModeloDto>(sql, new
            {
                EmpresaId = empresaId,
                PaginaModeloId = paginaModeloId
            });
        }

        public async Task<IEnumerable<CampoOpcaoDto>> ListarOpcoesAsync(
            int empresaId, int campoModeloId)
        {
            using var conn = await  _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT Id, CampoModeloId, Texto, Valor, Ordem, Ativo
                FROM CampoConfiguracoes
                WHERE EmpresaId = @EmpresaId
                  AND CampoModeloId = @CampoModeloId
                ORDER BY Ordem;";

            return await conn.QueryAsync<CampoOpcaoDto>(sql, new
            {
                EmpresaId = empresaId,
                CampoModeloId = campoModeloId
            });
        }
    }
}
