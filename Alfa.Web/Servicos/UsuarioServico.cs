using Alfa.Web.Dtos;
using Alfa.Web.Models;
using Alfa.Web.Servicos.Interfaces;

namespace Alfa.Web.Servicos
{
    public class UsuarioServico : IUsuarioServico
    {
        private readonly IHttpClientFactory _httpFactory;

        public UsuarioServico(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        public async Task<IEnumerable<UsuarioEmpresaViewModel>> ListarUsuariosEmpresaAsync(int empresaId)
        {
            string rotaListarUsuariosEmpresa = $"/api/usuario/{empresaId}/listar";

            var client = _httpFactory.CreateClient("AlfaApi");
            var resposta = await client.GetAsync(rotaListarUsuariosEmpresa);

            if (!resposta.IsSuccessStatusCode)
            {
                return [];
            }

            var usuarios = await resposta.Content.ReadFromJsonAsync<IEnumerable<UsuarioEmpresaViewModel>>();
            return usuarios ?? [];
        }

        public async Task<(bool Success, string? Error)> RegistrarAsync(UsuarioRegistroDto registro)
        {
            var client = _httpFactory.CreateClient("AlfaApi");
            var resposta = await client.PostAsJsonAsync("/api/usuario/registrar", registro);

            if (resposta.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var txt = await resposta.Content.ReadAsStringAsync();
            return (false, txt);
        }
    }
}
