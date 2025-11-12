using System.Net.Http.Headers;

namespace Alfa.Web.Servicos;

public class EmpresaHeaderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _ctx;
    public EmpresaHeaderHandler(IHttpContextAccessor ctx) => _ctx = ctx;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        var ctx = _ctx.HttpContext;
        string? jwt = null;

        if (ctx?.Request.Cookies.TryGetValue("JwtToken", out var fromCookie) == true)
            jwt = fromCookie;

        if (string.IsNullOrEmpty(jwt))
            jwt = ctx?.User?.FindFirst("JwtToken")?.Value;

        if (!string.IsNullOrEmpty(jwt))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        return await base.SendAsync(req, ct);
    }
}
