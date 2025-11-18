using System.Net.Http;
using System.Net.Http.Json;
using Alfa.Web.Dtos;
using Alfa.Web.Models;
using Alfa.Web.Servicos.Interfaces;

namespace Alfa.Web.Servicos;

public class ProcessoServico : IProcessoServico
{
    private readonly HttpClient _httpClient;

    public ProcessoServico(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("AlfaApi");
    }

    public async Task<PaginadoResultadoDto<ProcessoListaItemViewModel>?> ObterProcessosAsync(int page, int pageSize, string? status)
    {
        var url = $"api/processos?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status))
        {
            url += $"&status={Uri.EscapeDataString(status)}";
        }

        var resposta = await _httpClient.GetAsync(url);
        if (!resposta.IsSuccessStatusCode)
        {
            var body = await resposta.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"API /processos falhou: {(int)resposta.StatusCode} {resposta.ReasonPhrase}. Corpo: {body}");
        }

        return await resposta.Content.ReadFromJsonAsync<PaginadoResultadoDto<ProcessoListaItemViewModel>>();
    }

    public Task<ProcessoDetalheViewModel?> ObterProcessoAsync(int id)
        => _httpClient.GetFromJsonAsync<ProcessoDetalheViewModel>($"api/processos/{id}");

    public Task<HttpResponseMessage> CriarProcessoAsync(string titulo, int[] fasesTemplateIds)
        => _httpClient.PostAsJsonAsync("api/processos", new { titulo, faseModeloIds = fasesTemplateIds });

    public Task<HttpResponseMessage> RegistrarRespostasAsync(int processoId, PaginaRespostaInput payload, string? preenchimentoToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/processos/{processoId}/respostas")
        {
            Content = JsonContent.Create(payload)
        };

        if (!string.IsNullOrWhiteSpace(preenchimentoToken))
        {
            request.Headers.Add("X-Preenchimento-Token", preenchimentoToken);
        }

        return _httpClient.SendAsync(request);
    }

    public Task<List<ProcessoPadraoModeloViewModel>?> ObterProcessoPadraoModelosAsync()
        => _httpClient.GetFromJsonAsync<List<ProcessoPadraoModeloViewModel>>("api/processos/padroes");

    public Task<HttpResponseMessage> CriarProcessoPadraoAsync(ProcessoPadraoModeloInput payload)
        => _httpClient.PostAsJsonAsync("api/processos/padroes", payload);

    public Task<HttpResponseMessage> AtualizarStatusAsync(int id, ProcessoStatusAtualizarInput payload)
        => _httpClient.PutAsJsonAsync($"api/processos/{id}/status", payload);

    public Task<List<ProcessoHistoricoViewModel>?> ObterHistoricoAsync(int processoId)
        => _httpClient.GetFromJsonAsync<List<ProcessoHistoricoViewModel>>($"api/processos/{processoId}/historico");
}
