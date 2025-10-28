using Alfa.Api.Dtos;
using Alfa.Api.Infra.Interfaces;
using Alfa.Api.Modelos;
using Alfa.Api.Repositorios.Interfaces;
using Alfa.Api.Utils;
using Dapper;
using System.Data;

namespace Alfa.Api.Repositorios
{
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly IExecucaoDb _execucaoDb;

        public UsuarioRepositorio(IExecucaoDb execucaoDb)
        {
            _execucaoDb = execucaoDb;
        }

        public async Task<UsuarioModel?> BuscarUsuarioPorEmailAsync(string email)
        {
            const string sql = @"
                SELECT Id, Nome, Email, SenhaHash, FuncaoId, Ativo
                FROM Usuarios 
                WHERE Email = @Email
            ";

            DynamicParameters parameters = new();
            parameters.Add("@Email", email, DbType.String);

            return await _execucaoDb.QuerySingleAsync<UsuarioModel>(sql, parameters);
        }

        public async Task CadastrarUsuarioAsync(UsuarioRegistroDto usuarioRegistro)
        {
            const string sql = @"
                INSERT INTO Usuarios (Nome, Email, SenhaHash, FuncaoId)
                VALUES (@Nome, @Email, @SenhaHash, @FuncaoId);
                SELECT CAST(SCOPE_IDENTITY() as int);
            ";

            DynamicParameters parameters = new();
            parameters.Add("@Nome", usuarioRegistro.Nome, DbType.String);
            parameters.Add("@Email", usuarioRegistro.Email, DbType.String);
            parameters.Add("@SenhaHash", SenhaHash.HashSenha(usuarioRegistro.Email, usuarioRegistro.Senha), DbType.String);
            parameters.Add("@FuncaoId", usuarioRegistro.FuncaoId, DbType.Int64);

            await _execucaoDb.ExecuteAsync(sql, parameters);
        }
    }
}
