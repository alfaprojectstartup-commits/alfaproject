using System.Data;

namespace Alfa.Api.Infra.Interfaces;
public interface IConexaoSql
{
    Task<IDbConnection> AbrirConexaoAsync(CancellationToken ct = default);
}