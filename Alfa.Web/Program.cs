using Polly;
using Polly.Extensions.Http;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// HttpClient tipado p/ API com resilência
var httpBuilder = builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
});

httpBuilder.AddPolicyHandler(
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retry => TimeSpan.FromMilliseconds(200 * retry))
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
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Processos}/{action=Index}/{id?}");

app.Run();

public class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(HttpClient http) => _http = http;

    public async Task<(int total, IEnumerable<dynamic> items)> GetProcessesAsync(int empresaId, int page = 1, int pageSize = 10)
    {
        var res = await _http.GetFromJsonAsync<ApiPaged>("api/processes?empresaId=" + empresaId + $"&page={page}&pageSize={pageSize}");
        return (res!.total, res.items);
    }

    public Task<HttpResponseMessage> CreateProcessAsync(string titulo, int empresaId) =>
        _http.PostAsJsonAsync("api/processes", new { titulo, empresaId });

    private record ApiPaged(int total, List<dynamic> items);
}
