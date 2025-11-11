using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Alfa.Api.Autorizacao
{
    /// <summary>
    /// Cria policies dinamicamente com base no prefixo "Permissao:".
    /// </summary>
    public class PermissaoPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback;

        public PermissaoPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallback = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith("Permissao:", StringComparison.OrdinalIgnoreCase))
            {
                var codigo = policyName.Substring("Permissao:".Length);
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissaoRequirement(codigo))
                    .Build();
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            return _fallback.GetPolicyAsync(policyName);
        }
    }
}
