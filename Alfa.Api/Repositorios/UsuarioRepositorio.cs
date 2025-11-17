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

        public async Task<UsuarioEmpresaDto?> BuscarUsuarioPorIdAsync(int usuarioId)
        {
            const string sql = @"
                SELECT Id, Nome, Email, EmpresaId, Ativo
                FROM Usuarios 
                WHERE Id = @UsuarioId
            ";

            DynamicParameters parameters = new();
            parameters.Add("@UsuarioId", usuarioId, DbType.Int64);

            return await _execucaoDb.QuerySingleAsync<UsuarioEmpresaDto>(sql, parameters);
        }

        public async Task<UsuarioModel?> BuscarUsuarioPorEmailAsync(string email)
        {
            const string sql = @"
                SELECT Id, Nome, Email, SenhaHash, EmpresaId, Ativo
                FROM Usuarios 
                WHERE Email = @Email
            ";

            DynamicParameters parameters = new();
            parameters.Add("@Email", email, DbType.String);

            return await _execucaoDb.QuerySingleAsync<UsuarioModel>(sql, parameters);
        }

        public async Task<IEnumerable<UsuarioEmpresaDto>> ListarUsuariosEmpresaAsync(int empresaId)
        {
            const string sql = @"
                SELECT Id, Nome, Email, EmpresaId, Ativo
                FROM Usuarios 
                WHERE EmpresaId = @EmpresaId
            ";

            DynamicParameters parameters = new();
            parameters.Add("@EmpresaId", empresaId, DbType.String);

            return await _execucaoDb.QueryAsync<UsuarioEmpresaDto>(sql, parameters);
        }

        public async Task<int> CadastrarUsuarioAsync(UsuarioRegistroDto usuarioRegistro)
        {
            const string sql = @"
                INSERT INTO Usuarios (Nome, Email, SenhaHash, EmpresaId)
                VALUES (@Nome, @Email, @SenhaHash, @EmpresaId);
                SELECT CAST(SCOPE_IDENTITY() as int);
            ";

            DynamicParameters parameters = new();
            parameters.Add("@Nome", usuarioRegistro.Nome, DbType.String);
            parameters.Add("@Email", usuarioRegistro.Email, DbType.String);
            parameters.Add("@SenhaHash", SenhaHash.HashSenha(usuarioRegistro.Email, usuarioRegistro.Senha), DbType.String);
            parameters.Add("@EmpresaId", usuarioRegistro.EmpresaId, DbType.Int64);

            return await _execucaoDb.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<int> AtualizarDadosUsuarioAsync(UsuarioEmpresaDto usuario)
        {
            const string sql = @"
                UPDATE Usuarios
                SET Nome = @Nome, Email = @Email, Ativo = CASE WHEN @Ativo = 1 THEN 1 ELSE 0 END
                WHERE Id = @UsuarioId;
            ";

            DynamicParameters parameters = new();
            parameters.Add("@Nome", usuario.Nome, DbType.String);
            parameters.Add("@Email", usuario.Email, DbType.String);
            parameters.Add("@UsuarioId", usuario.Id, DbType.Int64);
            parameters.Add("@Ativo", usuario.Ativo, DbType.Boolean);

            return await _execucaoDb.ExecuteAsync(sql, parameters);
        }

        public async Task<IEnumerable<string>> ObterPermissoesPorUsuarioIdAsync(int usuarioId)
        {
            const string sql = @"
                SELECT PE.Codigo
                FROM Permissoes PE
                INNER JOIN UsuariosPermissoes UP
                    ON UP.PermissaoId = PE.Id
                WHERE UP.UsuarioId = @UsuarioId;
            ";

            DynamicParameters parameters = new();
            parameters.Add("@UsuarioId", usuarioId, DbType.Int64);

            var resultado = await _execucaoDb.QueryAsync<string>(sql, parameters);
            return resultado.Distinct().ToList();
        }

        public async Task ConcederPermissaoAsync(int usuarioId, int permissaoId, int? concedidoPor)
        {
            const string sql = @"
                  IF NOT EXISTS (
                    SELECT 1 FROM UsuariosPermissoes WHERE UsuarioId = @UsuarioId AND PermissaoId = @PermissaoId
                  )
                  INSERT INTO UsuariosPermissoes (UsuarioId, PermissaoId, ConcedidoEm, ConcedidoPor)
                  VALUES (@UsuarioId, @PermissaoId, SYSUTCDATETIME(), @ConcedidoPor);
            ";

            DynamicParameters parameters = new();
            parameters.Add("@UsuarioId", usuarioId, DbType.Int64);
            parameters.Add("@PermissaoId", permissaoId, DbType.Int64);
            parameters.Add("@ConcedidoPor", concedidoPor, DbType.Int64);

            await _execucaoDb.ExecuteAsync(sql, parameters);
        }

        public async Task RevogarPermissaoAsync(int usuarioId, int permissaoId)
        {
            const string sql = @"
                DELETE FROM UsuariosPermissoes 
                WHERE UsuarioId = @UsuarioId AND PermissaoId = @PermissaoId;
            ";

            DynamicParameters parameters = new();
            parameters.Add("@UsuarioId", usuarioId, DbType.Int64);
            parameters.Add("@PermissaoId", permissaoId, DbType.Int64);

            await _execucaoDb.ExecuteAsync(sql, parameters);
        }
    }
}
