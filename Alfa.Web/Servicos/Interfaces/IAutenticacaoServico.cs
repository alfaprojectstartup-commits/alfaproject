using Alfa.Web.Dtos;

namespace Alfa.Web.Servicos.Interfaces
{
    public interface IAutenticacaoServico
    {
        Task<UsuarioAutenticadoDto?> LoginAsync(UsuarioLoginDto login);
    }
}
