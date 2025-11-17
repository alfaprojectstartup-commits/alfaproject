using Alfa.Api.Dtos;
using Alfa.Api.Modelos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface IUsuarioRepositorio
    {
        Task<UsuarioEmpresaDto?> BuscarUsuarioPorIdAsync(int usuarioId);
        Task<UsuarioModel?> BuscarUsuarioPorEmailAsync(string email);
        Task<IEnumerable<UsuarioEmpresaDto>> ListarUsuariosEmpresaAsync(int empresaId);
        Task<int> CadastrarUsuarioAsync(UsuarioRegistroDto usuarioRegistro);
        Task<int> AtualizarDadosUsuarioAsync(UsuarioEmpresaDto usuario);
        Task<IEnumerable<string>> ObterPermissoesPorUsuarioIdAsync(int usuarioId);
        Task ConcederPermissaoAsync(int usuarioId, int permissaoId, int? concedidoPor);
        Task RevogarPermissaoAsync(int usuarioId, int permissaoId);
    }
}
