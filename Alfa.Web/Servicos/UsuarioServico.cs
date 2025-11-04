using Alfa.Web.Dtos;
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

        public async Task<UsuarioAutenticadoDto?> LoginAsync(UsuarioLoginDto login)
        {
            var client = _httpFactory.CreateClient("AlfaApi");
            var resp = await client.PostAsJsonAsync("/api/usuario/login", login);

            if (!resp.IsSuccessStatusCode) {
                return null;
            } 

            var auth = await resp.Content.ReadFromJsonAsync<UsuarioAutenticadoDto>();
            return auth;
        }

        public async Task<(bool Success, string? Error)> RegistrarAsync(UsuarioRegistroDto registro)
        {
            var client = _httpFactory.CreateClient("AlfaApi");
            var resp = await client.PostAsJsonAsync("/api/usuario/registrar", registro);

            if (resp.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var txt = await resp.Content.ReadAsStringAsync();
            return (false, txt);
        }
    }
}
