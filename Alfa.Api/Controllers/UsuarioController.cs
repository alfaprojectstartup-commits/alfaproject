using Alfa.Api.Dtos;
using Alfa.Api.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alfa.Api.Controllers
{
    [ApiController]
    [Route("api/usuario")]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioServico _usuarioServico;

        public UsuarioController(IUsuarioServico usuarioServico)
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

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] UsuarioRegistroDto usuarioRegistro)
        {
            await _usuarioServico.CadastrarUsuarioAsync(usuarioRegistro);
            return Created();
        }

        [Authorize]
        [HttpGet("permissoes")]
        public async Task<IActionResult> ObterPermissoesUsuarios()
        {
            var usuarioIdClaim = User.FindFirst("usuarioId")?.Value;

            if (!int.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Forbid();
            }
                
            var permissoes = await _usuarioServico.ObterPermissoesPorUsuarioAsync(usuarioId);
            return Ok(permissoes);
        }

    }
}
