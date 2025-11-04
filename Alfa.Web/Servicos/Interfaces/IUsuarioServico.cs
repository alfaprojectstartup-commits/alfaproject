using Alfa.Web.Dtos;

namespace Alfa.Web.Servicos.Interfaces
{
    public interface IUsuarioServico
    {
        Task<UsuarioAutenticadoDto?> LoginAsync(UsuarioLoginDto login);
        Task<(bool Success, string? Error)> RegistrarAsync(UsuarioRegistroDto registro);
    }
}
