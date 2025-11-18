using Alfa.Web.Dtos;
using Alfa.Web.Models;
using Alfa.Web.Servicos.Interfaces;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Alfa.Web.Servicos
{
    public class PermissaoUiServico : IPermissaoUiServico
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _contextAccessor;

        public PermissaoUiServico(IHttpClientFactory httpClientFactory, IHttpContextAccessor contextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _contextAccessor = contextAccessor;
        }

        public async Task<IEnumerable<PermissaoViewModel>> ListarPermissoesSistemaAsync()
        {
            string rotaPermissoesSistema = "/api/usuario/permissoes";

            var client = _httpClientFactory.CreateClient("AlfaApi");
            var resposta = await client.GetAsync(rotaPermissoesSistema);

            if (!resposta.IsSuccessStatusCode)
            {
                return [];
            }

            var usuario = await resposta.Content.ReadFromJsonAsync<IEnumerable<PermissaoViewModel>>();
            return usuario ?? [];
        }

        public async Task<HashSet<string>> ObterPermissoesAsync(string token)
        {
            var context = _contextAccessor.HttpContext!;
            var client = _httpClientFactory.CreateClient("AlfaApi");

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
                
            var response = await client.GetAsync("/api/usuario/permissoes/interface");
            if (!response.IsSuccessStatusCode) 
            {
                return [];
            }
            
            var json = await response.Content.ReadAsStringAsync();

            var permissoesDto = JsonSerializer.Deserialize<PermissoesUsuariosDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (permissoesDto is null || permissoesDto.Permissoes is null)
            {
                return [];
            }

            var permissoes = new HashSet<string>(permissoesDto.Permissoes);

            context.Session.SetString("Permissoes", JsonSerializer.Serialize(permissoes));
            return permissoes;
        }

        public bool PossuiPermissao(string codigo)
        {
            var context = _contextAccessor.HttpContext!;
            var json = context.Session.GetString("Permissoes");
            if (string.IsNullOrEmpty(json)) {
                return false;
            }

            var permissoes = JsonSerializer.Deserialize<HashSet<string>>(json);
            return permissoes?.Contains(codigo) ?? false;
        }

    }
}
