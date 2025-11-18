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

        [HttpPut("{usuarioId}")]
        public async Task<IActionResult> AtualizarDadosUsuarioAsync([FromBody] UsuarioEmpresaDto usuario)
        {
            await _usuarioServico.AtualizarDadosUsuarioAsync(usuario);
            return Ok();
        }

        [HttpGet("permissoes")]
        public async Task<IActionResult> ListarPermissoesSistemaAsync()
        {
            var permissoes = await _usuarioServico.ListarPermissoesSistemaAsync();
            return Ok(permissoes);
        }

        [HttpGet("permissoes/interface")]
        public async Task<IActionResult> ObterUsuarioPermissoesUiAsync()
        {
            var usuarioIdClaim = User.FindFirst("usuarioId")?.Value;

            if (!int.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Forbid();
            }
                
            var permissoes = await _usuarioServico.ObterUsuarioPermissoesUiAsync(usuarioId);
            return Ok(permissoes);
        }

        [HttpGet("permissoes/{usuarioId}")]
        public async Task<IActionResult> ObterPermissoesUsuarioAsync([FromRoute] int usuarioId)
        {
            var permissoes = await _usuarioServico.ObterPermissoesUsuarioAsync(usuarioId);
            return Ok(permissoes);
        }
    }
}
