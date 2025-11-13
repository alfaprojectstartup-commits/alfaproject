using System.Net.Http.Json;
using Alfa.Web.Dtos;
using Alfa.Web.Models;

namespace Alfa.Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AlfaApi"); // <- usa o client nomeado COM handler
    }

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

    public Task<ProcessoDetalheViewModel?> GetProcessoAsync(int id)
        => _http.GetFromJsonAsync<ProcessoDetalheViewModel>($"api/processos/{id}");

    public Task<List<FaseInstanciaViewModel>?> GetProcessoFasesAsync(int id)
        => _http.GetFromJsonAsync<List<FaseInstanciaViewModel>>($"api/processos/{id}/fases");

    public Task<HttpResponseMessage> CriarProcessoAsync(string titulo, int[] fasesTemplateIds)
        => _http.PostAsJsonAsync("api/processos", new { titulo, faseModeloIds = fasesTemplateIds });

    public Task<HttpResponseMessage> RegistrarRespostasAsync(int processoId, PaginaRespostaInput payload)
        => _http.PostAsJsonAsync($"api/processos/{processoId}/respostas", payload);

    public Task<List<ProcessoPadraoModeloViewModel>?> GetProcessoPadraoModelosAsync()
        => _http.GetFromJsonAsync<List<ProcessoPadraoModeloViewModel>>("api/processos/padroes");

    public Task<HttpResponseMessage> CriarProcessoPadraoAsync(ProcessoPadraoModeloInput payload)
        => _http.PostAsJsonAsync("api/processos/padroes", payload);

    public Task<List<FaseModelosViewModel>?> GetFaseTemplatesAsync()
        => _http.GetFromJsonAsync<List<FaseModelosViewModel>>("api/fases/modelos");

    public Task<FaseModelosViewModel?> GetFaseTemplateAsync(int id)
        => _http.GetFromJsonAsync<FaseModelosViewModel>($"api/fases/modelos/{id}");

    public Task<HttpResponseMessage> CriarFaseTemplateAsync(FaseTemplateInputDto payload)
        => _http.PostAsJsonAsync("api/fases/modelos", payload);

    public Task<HttpResponseMessage> AtualizarFaseTemplateAsync(int id, FaseTemplateInputDto payload)
        => _http.PutAsJsonAsync($"api/fases/modelos/{id}", payload);

    public Task<HttpResponseMessage> AtualizarProcessoStatusAsync(int id, ProcessoStatusAtualizarInput payload)
        => _http.PutAsJsonAsync($"api/processos/{id}/status", payload);

    public Task<List<ProcessoHistoricoViewModel>?> GetProcessoHistoricoAsync(int id)
        => _http.GetFromJsonAsync<List<ProcessoHistoricoViewModel>>($"api/processos/{id}/historico");
}
