using Alfa.Api.Repositorios.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System.IdentityModel.Tokens.Jwt;

namespace Alfa.Api.Autorizacao
{
    /// <summary>
    /// Handler responsável por validar se o usuário possui determinada permissão.
    /// </summary>
    public class PermissaoHandler: AuthorizationHandler<PermissaoRequirement>
    {
        private readonly IUsuarioRepositorio _usuarioRepositorio;
        private readonly IMemoryCache _cache;

        public PermissaoHandler(IUsuarioRepositorio usuarioRepositorio, IMemoryCache cache)
        {
            _usuarioRepositorio = usuarioRepositorio;
            _cache = cache;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissaoRequirement requirement)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                context.Fail();
                return;
            }

            var usuarioIdClaim = context.User.FindFirst("usuarioId")?.Value
                                 ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (!int.TryParse(usuarioIdClaim, out var usuarioId))
            {
                context.Fail();
                return;
            }

            var cacheKey = $"permissoes_usuario_{usuarioId}";
            var permissoes = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                return await _usuarioRepositorio.ObterUsuarioPermissoesUiAsync(usuarioId);
            });

            if (permissoes is not null && permissoes.Contains(requirement.Codigo))
                context.Succeed(requirement);
            else
                context.Fail();
        }
    }
}
