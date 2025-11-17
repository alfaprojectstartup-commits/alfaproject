using Alfa.Api.Autorizacao;
using Alfa.Api.Dtos;
using Alfa.Api.Servicos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alfa.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/usuario")]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioServico _usuarioServico;

        public UsuarioController(IUsuarioServico usuarioServico)
        {
            _usuarioServico = usuarioServico;
        }

        [Authorize(Policy = Permissoes.UsuariosGerenciar)]
        [HttpGet("{usuarioId}")]
        public async Task<IActionResult> BuscarUsuarioPorIdAsync([FromRoute] int usuarioId)
        {
            return Ok(await _usuarioServico.BuscarUsuarioPorIdAsync(usuarioId));
        }

        [Authorize(Policy = Permissoes.UsuariosGerenciar)]
        [HttpGet("{empresaId}/listar")]
        public async Task<IActionResult> ListarUsuariosEmpresaAsync([FromRoute] int empresaId)
        {
            return Ok(await _usuarioServico.ListarUsuariosEmpresaAsync(empresaId));
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] UsuarioRegistroDto usuarioRegistro)
        {
            await _usuarioServico.CadastrarUsuarioAsync(usuarioRegistro);
            return Created();
        }

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
