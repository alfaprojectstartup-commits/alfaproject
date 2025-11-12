using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;

namespace Alfa.Web.Servicos.Handlers;

public class JwtCookieHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtCookieHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;

        if (context != null && context.Request.Cookies.TryGetValue("JwtToken", out var jwtToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var empresaId = context.User?.FindFirst("empresaId")?.Value;
            if (string.IsNullOrWhiteSpace(empresaId))
            {
                try
                {
                    var token = new JwtSecurityTokenHandler().ReadJwtToken(jwtToken);
                    empresaId = token.Claims
                        .FirstOrDefault(c => c.Type.Equals("empresaId", StringComparison.OrdinalIgnoreCase))?
                        .Value;
                }
                catch (ArgumentException)
                {
                    // Token inválido, segue sem enviar o cabeçalho da empresa.
                }
            }

            if (!string.IsNullOrWhiteSpace(empresaId))
            {
                request.Headers.Remove("X-Empresa-Id");
                request.Headers.Add("X-Empresa-Id", empresaId);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
