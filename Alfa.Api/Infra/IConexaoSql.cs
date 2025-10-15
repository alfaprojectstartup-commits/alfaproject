using System.Data;

namespace Alfa.Api.Dados;
public interface IConexaoSql
{
    Task<IDbConnection> AbrirAsync(CancellationToken ct = default);
}