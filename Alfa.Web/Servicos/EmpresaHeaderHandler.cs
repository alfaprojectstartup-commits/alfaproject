namespace Alfa.Web.Servicos;

public class EmpresaHeaderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _ctx;
    public EmpresaHeaderHandler(IHttpContextAccessor ctx) => _ctx = ctx;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        var empId = _ctx.HttpContext?.Session.GetInt32("EmpresaId");
        if (empId is > 0)
        {
            req.Headers.Remove("X-Empresa-Id");
            req.Headers.Add("X-Empresa-Id", empId.Value.ToString());
        }
        return base.SendAsync(req, ct);
    }
}
