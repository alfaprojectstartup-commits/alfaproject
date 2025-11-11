using Alfa.Web.Dtos;
using Alfa.Web.Servicos.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Alfa.Web.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly IUsuarioServico _usuarioServico;

        public UsuarioController(IUsuarioServico usuarioServico)
        {
            _usuarioServico = usuarioServico;
        }

        public async Task<IActionResult> Index()
        {
            var empresaIdClaim = User.FindFirst("empresaId")?.Value;

            if (!int.TryParse(empresaIdClaim, out var empresaId))
            {
                return Forbid();
            }

            var usuarios = await _usuarioServico.ListarUsuariosEmpresaAsync(empresaId);
            return View(usuarios);
        }

        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(UsuarioRegistroDto model)
        {
            var empresaIdClaim = User.FindFirst("empresaId")?.Value;

            if (!int.TryParse(empresaIdClaim, out var empresaId))
            {
                return Forbid();
            }

            model.EmpresaId = empresaId;

            if (!ModelState.IsValid) return View(model);

            var (success, error) = await _usuarioServico.RegistrarAsync(model);
            if (!success)
            {
                ModelState.AddModelError("", "Erro ao registrar: " + (error ?? "Desconhecido"));
                return View(model);
            }

            // após registrar, redirecionar para login com mensagem
            TempData["Message"] = "Registro realizado com sucesso. Faça login.";
            return RedirectToAction("Index");
        }
    }
}
