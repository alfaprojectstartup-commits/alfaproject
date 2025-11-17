using Alfa.Api.Dtos;

namespace Alfa.Api.Servicos.Interfaces
{
    public interface IUsuarioServico
    {
        Task<UsuarioAutenticadoDto?> Login(UsuarioLoginDto login);
        Task<UsuarioEmpresaDto?> BuscarUsuarioPorIdAsync(int usuarioId);
        Task<IEnumerable<UsuarioEmpresaDto>> ListarUsuariosEmpresaAsync(int empresaId);
        Task CadastrarUsuarioAsync(UsuarioRegistroDto usuario);
        Task<PermissoesUsuarioDto> ObterPermissoesPorUsuarioAsync(int usuarioId);
        Task AtualizarDadosUsuarioAsync(UsuarioEmpresaDto usuario);
    }
}
