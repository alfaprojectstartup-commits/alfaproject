using Alfa.Api.Infra.Interfaces;
using Alfa.Api.Modelos;
using Alfa.Api.Repositorios.Interfaces;
using Dapper;
using System.Data;

namespace Alfa.Api.Repositorios
{
    public class UsuarioRepositorio(IExecucaoDb execucaoDb) : IUsuarioRepositorio
    {
        private readonly IExecucaoDb _execucaoDb = execucaoDb;

        public async Task<UsuarioModel?> BuscarUsuarioPorEmailAsync(string email)
        {
            const string sql = @"SELECT Id, Nome, Email, SenhaHash, FuncaoId, Ativo
                                FROM Usuarios 
                                WHERE Email = @Email";

            DynamicParameters parameters = new();
            parameters.Add("@Email", email, DbType.String);

            return await _execucaoDb.QuerySingleAsync<UsuarioModel>(sql, parameters);
        }
    }
}
