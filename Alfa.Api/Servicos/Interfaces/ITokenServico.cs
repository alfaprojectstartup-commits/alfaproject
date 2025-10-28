using Alfa.Api.Modelos;

namespace Alfa.Api.Servicos.Interfaces
{
    public interface ITokenServico
    {
        string GerarToken(UsuarioModel usuario);
    }
}
