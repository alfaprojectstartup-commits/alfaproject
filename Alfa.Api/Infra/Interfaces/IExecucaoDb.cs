using Dapper;
using System.Data;

namespace Alfa.Api.Infra.Interfaces
{
    public interface IExecucaoDb
    {
        Task<T?> QuerySingleAsync<T>(string sql, DynamicParameters? parametros = null, IDbTransaction? transacao = null);
        Task<IEnumerable<T>> QueryAsync<T>(string sql, DynamicParameters? parametros = null, IDbTransaction? transacao = null);
        Task<int> ExecuteAsync(string sql, DynamicParameters? parametros = null, IDbTransaction? transacao = null);
        Task<T?> ExecuteScalarAsync<T>(string sql, DynamicParameters? parametros = null, IDbTransaction? transacao = null);
    }
}
