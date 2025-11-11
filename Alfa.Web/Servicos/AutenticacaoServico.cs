using Alfa.Web.Dtos;
using Alfa.Web.Servicos.Interfaces;

namespace Alfa.Web.Servicos
{
    public class AutenticacaoServico : IAutenticacaoServico
    {
        private readonly IHttpClientFactory _httpFactory;

        public AutenticacaoServico(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        public async Task<UsuarioAutenticadoDto?> LoginAsync(UsuarioLoginDto login)
        {
            var client = _httpFactory.CreateClient("AlfaApiLogin");
            var resp = await client.PostAsJsonAsync("/api/autenticacao/login", login);

            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            var auth = await resp.Content.ReadFromJsonAsync<UsuarioAutenticadoDto>();
            return auth;
        }
    }
}
