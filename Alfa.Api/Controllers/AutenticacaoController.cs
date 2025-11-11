using Alfa.Api.Dtos;
using Alfa.Api.Servicos.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Alfa.Api.Controllers
{
    [ApiController]
    [Route("api/autenticacao")]
    public class AutenticacaoController : ControllerBase
    {
        private readonly IUsuarioServico _usuarioServico;

        public AutenticacaoController(IUsuarioServico usuarioServico)
        {
            _usuarioServico = usuarioServico;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UsuarioLoginDto login)
        {
            var logado = await _usuarioServico.Login(login);
            if (logado == null)
            {
                return Unauthorized("E-mail ou senha inválidos.");
            }

            return Ok(logado);
        }
    }
}
