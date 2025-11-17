using Alfa.Api.Dtos;
using Alfa.Api.Modelos;

namespace Alfa.Api.Servicos.Interfaces
{
    public interface IUsuarioServico
    {
        Task<UsuarioAutenticadoDto?> Login(UsuarioLoginDto login);
        Task<UsuarioEmpresaDto?> BuscarUsuarioPorIdAsync(int usuarioId);
        Task<IEnumerable<UsuarioEmpresaDto>> ListarUsuariosEmpresaAsync(int empresaId);
        Task CadastrarUsuarioAsync(UsuarioRegistroDto usuario);
        Task<IEnumerable<PermissaoModel?>> ListarPermissoesSistemaAsync();
        Task<UsuarioPermissoesUiDto> ObterUsuarioPermissoesUiAsync(int usuarioId);
        Task<IEnumerable<UsuarioPermissaoModel?>> ObterPermissoesUsuarioAsync(int usuarioId);
        Task AtualizarDadosUsuarioAsync(UsuarioEmpresaDto usuario);
    }
}
