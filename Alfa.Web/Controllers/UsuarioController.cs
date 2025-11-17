using Alfa.Web.Dtos;
using Alfa.Web.Models;
using Alfa.Web.Servicos.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Alfa.Web.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly IUsuarioServico _usuarioServico;

        public UsuarioController(IUsuarioServico usuarioServico)
        {
            _usuarioServico = usuarioServico;
        }

        public async Task<IActionResult> Index(int pagina = 1)
        {
            var empresaIdClaim = User.FindFirst("empresaId")?.Value;

            if (!int.TryParse(empresaIdClaim, out var empresaId))
            {
                return Forbid();
            }

            var usuarios = await _usuarioServico.ListarUsuariosEmpresaAsync(empresaId);
            var usuariosOrdenados = usuarios.OrderBy(x => x.Nome).ToList();

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            ViewData["UsuariosJson"] = JsonSerializer.Serialize(usuariosOrdenados, jsonOptions);

            int totalUsuarios = usuariosOrdenados.Count();
            int itensPorPagina = 10;

            var usuariosPaginados = usuariosOrdenados
                .Skip((pagina - 1) * itensPorPagina)
                .Take(itensPorPagina)
                .ToList();

            var viewModel = new UsuarioPaginacaoViewModel<UsuarioEmpresaViewModel>
            {
                Itens = usuariosPaginados,
                PaginaAtual = pagina,
                TotalPaginas = (int)Math.Ceiling(totalUsuarios / (double)itensPorPagina)
            };

            return View(viewModel);
        }

        // GET: /Usuario/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            var usuario = await _usuarioServico.ObterUsuarioPorIdAsync(id);
            if (usuario == null) {
                return NotFound();
            }
                
            var vm = new UsuarioEmpresaViewModel
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                EmpresaId = usuario.EmpresaId,
                Ativo = usuario.Ativo
            };

            return View("Editar", vm);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Editar(UsuarioEmpresaViewModel model)
        //{
        //    if (!ModelState.IsValid) {
        //        return View("Editar", model);
        //    } 

        //    var ok = await _usuarioServico.AtualizarUsuarioAsync(model);
        //    if (!ok)
        //    {
        //        ModelState.AddModelError("", "Erro ao salvar usuário");
        //        return View("Editar", model);
        //    }

        //    TempData["ok"] = "Usuário atualizado.";
        //    return RedirectToAction("Index");
        //}

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
