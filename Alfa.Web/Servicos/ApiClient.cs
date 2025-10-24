using System.Net.Http.Json;
using Alfa.Web.Dtos;
using Alfa.Web.Models;

namespace Alfa.Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(HttpClient http) => _http = http;

    public async Task<PaginadoResultadoDto<ProcessoListaItemViewModel>?> GetProcessosAsync(int page, int pageSize, string? status)
    {
        var url = $"api/processos?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";

        var resp = await _http.GetAsync(url);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"API /processos falhou: {(int)resp.StatusCode} {resp.ReasonPhrase}. Corpo: {body}");
        }
        return await resp.Content.ReadFromJsonAsync<PaginadoResultadoDto<ProcessoListaItemViewModel>>();
    }

    public Task<HttpResponseMessage> CriarProcessoAsync(string titulo, int[] fasesTemplateIds)
        => _http.PostAsJsonAsync("api/processos", new { titulo, fasesTemplateIds });

    public Task<List<FaseModelosViewModel>?> GetFaseTemplatesAsync()
        => _http.GetFromJsonAsync<List<FaseModelosViewModel>>("api/fases/templates");
}
