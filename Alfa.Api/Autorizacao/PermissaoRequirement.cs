using Microsoft.AspNetCore.Authorization;

namespace Alfa.Api.Autorizacao
{
    /// <summary>
    /// Representa um requisito de autorização baseado em código de permissão.
    /// </summary>
    public class PermissaoRequirement : IAuthorizationRequirement
    {
        public string Codigo { get; }
        public PermissaoRequirement(string codigo) => Codigo = codigo;
    }
}
