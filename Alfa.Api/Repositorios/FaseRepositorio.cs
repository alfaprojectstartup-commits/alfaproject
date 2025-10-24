using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Alfa.Api.Db;
using Alfa.Api.Dtos;
using Alfa.Api.Repositorios.Interfaces;
using Alfa.Api.Infra.Interfaces;

namespace Alfa.Api.Repositorios
{
    public class FaseRepositorio : IFaseRepositorio
    {
        private readonly IConexaoSql _db;
        private readonly IRespostaRepositorio _respostas;

        public FaseRepositorio(IConexaoSql db, IRespostaRepositorio respostas)
        {
            _db = db;
            _respostas = respostas;
        }

        public async Task<IEnumerable<FaseTemplateDto>> ListarTemplatesAsync(int empresaId)
        {
            using IDbConnection conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT Id, Titulo, Ordem
                FROM FaseTemplate
                WHERE EmpresaId = @EmpresaId
                ORDER BY Ordem;";

            return await conn.QueryAsync<FaseTemplateDto>(sql, new { EmpresaId = empresaId });
        }

        public async Task<IEnumerable<FaseInstanceDto>> ListarInstanciasAsync(int empresaId, int processoId)
        {
            using IDbConnection conn = await _db.AbrirConexaoAsync();
            const string sql = @"
                SELECT Id, ProcessoId, FaseTemplateId, Titulo, Ordem, Progresso
                FROM FaseInstance
                WHERE EmpresaId = @EmpresaId AND ProcessoId = @ProcessoId
                ORDER BY Ordem;";

            return await conn.QueryAsync<FaseInstanceDto>(sql, new
            {
                EmpresaId = empresaId,
                ProcessoId = processoId
            });
        }

        public async Task RecalcularProgressoFaseAsync(int empresaId, int faseInstanceId)
        {
            // pega, para cada página da fase, quantos obrigatórios existem e quantos estão preenchidos
            var resumo = await _respostas.ObterResumoPorPaginasDaFaseAsync(empresaId, faseInstanceId);

            double progresso = 0;
            var lista = resumo.ToList();
            if (lista.Count > 0)
            {
                var percentuais = lista.Select(x => x.obrig == 0 ? 100.0 : (100.0 * x.preenc / x.obrig));
                progresso = percentuais.Average();
            }

            // arredonda para inteiro (0..100) e salva na FaseInstance
            var progressoInt = (int)System.Math.Round(progresso);

            using var conn = await _db.AbrirConexaoAsync();
            const string updSql = @"
                UPDATE FaseInstance
                SET Progresso = @Progresso
                WHERE EmpresaId = @EmpresaId
                  AND Id = @FaseInstanceId;";

            await conn.ExecuteAsync(updSql, new
            {
                EmpresaId = empresaId,
                FaseInstanceId = faseInstanceId,
                Progresso = progressoInt
            });
        }
    }
}
