using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.Extensions.Http;
using Alfa.Web.Dtos;

var builder = WebApplication.CreateBuilder(args);

// MVC + Runtime Compilation (se for usar)
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// Sessão
builder.Services.AddDistributedMemoryCache(); // << faltava
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(8);
    o.Cookie.Name = ".Alfa.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<EmpresaHeaderHandler>();

// HttpClient tipado para a API + Resiliência + Header de Empresa (apenas UMA vez)
builder.Services.AddHttpClient<ApiClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
})
.AddHttpMessageHandler<EmpresaHeaderHandler>()
.AddPolicyHandler(
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            3,
            retry => TimeSpan.FromMilliseconds(200 * retry))
);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // << faltava aqui (depois de UseRouting)

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Processos}/{action=Index}/{id?}");

app.Run();

public class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(HttpClient http) => _http = http;

    public Task<PaginadoResultDto<ProcessoListItemVm>?> GetProcessosAsync(
        int page, int pageSize, string? status)
        => _http.GetFromJsonAsync<PaginadoResultDto<ProcessoListItemVm>>(
            $"api/processos?page={page}&pageSize={pageSize}&status={status}");

    public Task<HttpResponseMessage> CriarProcessoAsync(string titulo, int[] fasesTemplateIds)
        => _http.PostAsJsonAsync("api/processos", new { titulo, fasesTemplateIds });

    // demais métodos seguirão o mesmo padrão
}

public class EmpresaHeaderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _ctx;
    public EmpresaHeaderHandler(IHttpContextAccessor ctx) => _ctx = ctx;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        var empId = _ctx.HttpContext?.Session.GetInt32("EmpresaId");
        if (empId.HasValue && empId.Value > 0)
        {
            req.Headers.Remove("X-Empresa-Id");
            req.Headers.Add("X-Empresa-Id", empId.Value.ToString());
        }
        return base.SendAsync(req, ct);
    }
}

public class AuthController : Controller
{
    [HttpGet("/login-mock")]
    public IActionResult LoginMock(int empresaId = 1, int usuarioId = 1, string nome = "Usuário Demo")
    {
        HttpContext.Session.SetInt32("EmpresaId", empresaId);
        HttpContext.Session.SetInt32("UsuarioId", usuarioId);
        HttpContext.Session.SetString("Nome", nome);

        // redireciona para a home de Processos
        return RedirectToAction("Index", "Processos");
    }

    [HttpGet("/logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("LoginMock");
    }
}
