using Alfa.Api.Infra.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Alfa.Api.Dados;
public class ConexaoSql(IConfiguration cfg) : IConexaoSql
{
    private readonly string _cs = cfg.GetConnectionString("Padrao")!;
    public async Task<IDbConnection> AbrirConexaoAsync(CancellationToken ct = default)
    {
        var cnn = new SqlConnection(_cs);
        await cnn.OpenAsync(ct);
        return cnn;
    }
}