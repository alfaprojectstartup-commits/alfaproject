using System.Collections.Generic;
using System.Linq;
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

        public async Task<IEnumerable<CampoModeloDto>> ListarCatalogoAsync(int empresaId)
        {
            using var conn = await _db.AbrirConexaoAsync();
            const string sqlCampos = @"
                SELECT Id, PaginaModeloId, NomeCampo, Rotulo, Tipo, Obrigatorio, Ordem, Placeholder, Mascara, Ajuda
                FROM CampoModelos
                WHERE EmpresaId = @EmpresaId
                  AND Ativo = 1
                ORDER BY Rotulo, NomeCampo;";

            var campos = (await conn.QueryAsync<CampoModeloDto>(sqlCampos, new
            {
                EmpresaId = empresaId
            })).ToList();

            if (!campos.Any()) return campos;

            const string sqlOpcoes = @"
                SELECT Id, CampoModeloId, Texto, Valor, Ordem, Ativo
                FROM CampoConfiguracoes
                WHERE EmpresaId = @EmpresaId
                  AND CampoModeloId IN @Ids
                ORDER BY CampoModeloId, Ordem;";

            var opcoes = await conn.QueryAsync<CampoOpcaoDto>(sqlOpcoes, new
            {
                EmpresaId = empresaId,
                Ids = campos.Select(c => c.Id)
            });

            var opcoesAgrupadas = opcoes
                .GroupBy(o => o.CampoModeloId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var campo in campos)
            {
                if (opcoesAgrupadas.TryGetValue(campo.Id, out var lista))
                {
                    campo.Opcoes = lista;
                }
            }

            return campos;
        }

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
