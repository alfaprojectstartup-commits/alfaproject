using Alfa.Api.Infra.Interfaces;
using Dapper;
using System.Data;

namespace Alfa.Api.Infra
{
    public class ExecucaoDb : IExecucaoDb
    {
        protected readonly IConexaoSql _dbConexao;

        public ExecucaoDb(IConexaoSql dbConexao)
        {
            _dbConexao = dbConexao;
        }

        /// <summary>
        /// Retorna um único registro ou null.
        /// </summary>
        public async Task<T?> QuerySingleAsync<T>(
            string sql,
            DynamicParameters? parametros = null,
            IDbTransaction? transacao = null)
        {
            using var conn = await _dbConexao.AbrirConexaoAsync();
            return await conn.QueryFirstOrDefaultAsync<T>(sql, parametros, transacao);
        }

        /// <summary>
        /// Retorna uma coleção de registros.
        /// Uma lista de objetos.
        /// Uma lista vazia.
        /// </summary>
        public async Task<IEnumerable<T>> QueryAsync<T>(
            string sql,
            DynamicParameters? parametros = null,
            IDbTransaction? transacao = null)
        {
            using var conn = await _dbConexao.AbrirConexaoAsync();
            return await conn.QueryAsync<T>(sql, parametros, transacao);
        }

        /// <summary>
        /// Executa comandos INSERT, UPDATE ou DELETE.
        /// Retorna o número de linhas afetadas.
        /// </summary>
        public async Task<int> ExecuteAsync(
            string sql,
            DynamicParameters? parametros = null,
            IDbTransaction? transacao = null)
        {
            using var conn = await _dbConexao.AbrirConexaoAsync();
            return await conn.ExecuteAsync(sql, parametros, transacao);
        }

        /// <summary>
        /// Executa um comando SQL que retorna um valor escalar (ex: ID, COUNT, SUM, etc).
        /// </summary>
        public async Task<T?> ExecuteScalarAsync<T>(
            string sql,
            DynamicParameters? parametros = null,
            IDbTransaction? transacao = null)
        {
            using var conn = await _dbConexao.AbrirConexaoAsync();
            return await conn.ExecuteScalarAsync<T>(sql, parametros, transacao);
        }
    }
}
