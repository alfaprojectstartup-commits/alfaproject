using System.Net.Http;
using System.Net.Http.Json;
using Alfa.Web.Dtos;
using Alfa.Web.Models;
using Alfa.Web.Servicos.Interfaces;

namespace Alfa.Web.Servicos;

public class FaseServico : IFaseServico
{
    private readonly HttpClient _httpClient;

    public FaseServico(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("AlfaApi");
    }

    public Task<List<FaseModelosViewModel>?> ObterModelosAsync()
        => _httpClient.GetFromJsonAsync<List<FaseModelosViewModel>>("api/fases/modelos");

    public Task<FaseModelosViewModel?> ObterModeloPorIdAsync(int id)
        => _httpClient.GetFromJsonAsync<FaseModelosViewModel>($"api/fases/modelos/{id}");

    public Task<HttpResponseMessage> CriarModeloAsync(FaseTemplateInputDto payload)
        => _httpClient.PostAsJsonAsync("api/fases/modelos", payload);

    public Task<HttpResponseMessage> AtualizarModeloAsync(int id, FaseTemplateInputDto payload)
        => _httpClient.PutAsJsonAsync($"api/fases/modelos/{id}", payload);
}
