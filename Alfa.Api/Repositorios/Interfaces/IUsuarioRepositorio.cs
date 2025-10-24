using Alfa.Api.Modelos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface IUsuarioRepositorio
    {
        Task<UsuarioModel?> BuscarUsuarioPorEmailAsync(string email);
    }
}
