using Alfa.Api.Dtos;
using Alfa.Api.Modelos;

namespace Alfa.Api.Servicos.Interfaces
{
    public interface IUsuarioServico
    {
        Task<LoginTokenDto?> Login(LoginDto login);
        Task<UsuarioModel?> BuscarUsuarioPorEmailAsync(string email);
        Task CadastrarUsuarioAsync(UsuarioRegistroDto usuario);
    }
}
