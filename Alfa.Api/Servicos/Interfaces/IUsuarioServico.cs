using Alfa.Api.Dtos;
using Alfa.Api.Modelos;

namespace Alfa.Api.Servicos.Interfaces
{
    public interface IUsuarioServico
    {
        Task<UsuarioAutenticadoDto?> Login(UsuarioLoginDto login);
        Task<UsuarioModel?> BuscarUsuarioPorEmailAsync(string email);
        Task<IEnumerable<UsuarioEmpresaDto>> ListarUsuariosEmpresaAsync(int empresaId);
        Task CadastrarUsuarioAsync(UsuarioRegistroDto usuario);
        Task<PermissoesUsuarioDto> ObterPermissoesPorUsuarioAsync(int usuarioId);
    }
}
