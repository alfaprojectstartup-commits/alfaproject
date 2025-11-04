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

        // GET: /Usuario/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Usuario/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UsuarioLoginDto model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) {
                return View(model);
            } 

            var auth = await _usuarioServico.LoginAsync(model);
            if (auth == null || string.IsNullOrEmpty(auth.Token))
            {
                ModelState.AddModelError("", "Credenciais inválidas.");
                return View(model);
            }

            // salvar token em cookie seguro
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(1) // ajustar conforme expiracao do token
            };
            Response.Cookies.Append("JwtToken", auth.Token, cookieOptions);

            // opcional: podemos criar ClaimsPrincipal local a partir do token (se parsearmos)
            // Por simplicidade, apenas redirecionamos
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
                
            return RedirectToAction("Index", "Home"); // rota após login
        }

        // GET: /Usuario/Registrar
        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }

        // POST: /Usuario/Registrar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(UsuarioRegistroDto model)
        {
            if (!ModelState.IsValid) return View(model);

            var (success, error) = await _usuarioServico.RegistrarAsync(model);
            if (!success)
            {
                ModelState.AddModelError("", "Erro ao registrar: " + (error ?? "Desconhecido"));
                return View(model);
            }

            // após registrar, redirecionar para login com mensagem
            TempData["Message"] = "Registro realizado com sucesso. Faça login.";
            return RedirectToAction("Login");
        }

        // GET: /Usuario/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("JwtToken");
            return RedirectToAction("Login");
        }

    }
}
