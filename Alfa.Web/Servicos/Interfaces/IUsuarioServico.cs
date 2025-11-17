using Alfa.Web.Dtos;
using Alfa.Web.Models;

namespace Alfa.Web.Servicos.Interfaces
{
    public interface IUsuarioServico
    {
        Task<UsuarioEmpresaViewModel?> ObterUsuarioPorIdAsync(int usuarioId);
        Task<IEnumerable<UsuarioEmpresaViewModel>> ListarUsuariosEmpresaAsync(int empresaId);
        Task<(bool Success, string? Error)> RegistrarAsync(UsuarioRegistroDto registro);
        Task<(bool Success, string? Error)> AtualizarDadosUsuarioAsync(UsuarioEmpresaViewModel usuario);
    }
}
