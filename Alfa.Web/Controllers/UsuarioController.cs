using Alfa.Web.Dtos;
using Alfa.Web.Servicos.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Alfa.Web.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly IUsuarioServico _usuarioServico;
        private readonly IPermissaoUiService _permissaoUiServico;

        public UsuarioController(IUsuarioServico usuarioServico, IPermissaoUiService permissaoUiServico)
        {
            _usuarioServico = usuarioServico;
            _permissaoUiServico = permissaoUiServico;
        }

        // GET: /Usuario/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        //// POST: /Usuario/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UsuarioLoginDto model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var autenticacao = await _usuarioServico.LoginAsync(model);
            if (autenticacao == null || string.IsNullOrEmpty(autenticacao.Token))
            {
                ModelState.AddModelError(string.Empty, "Credenciais inválidas. Verifique e tente novamente.");
                return View(model);
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            ArmazenarTokenCookie(autenticacao.Token);
            await LogarUsuarioCookie(autenticacao.Token);
            await _permissaoUiServico.ObterPermissoesAsync(autenticacao.Token);
            
            return RedirectToAction("Index", "Home");
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
            return RedirectToAction("Login");
        }

        // GET: /Usuario/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            Response.Cookies.Delete("JwtToken");

            return RedirectToAction("Login", "Usuario");
        }

        private void ArmazenarTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                //Expires = DateTimeOffset.UtcNow.AddHours(8) // comentar somente em local, para o token expirar junto com a sessão
            };
            Response.Cookies.Append("JwtToken", token, cookieOptions);
        }

        private async Task LogarUsuarioCookie(string jwtToken)
        {
            // Decodifica o token
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwtToken);

            var claims = token.Claims.ToList();
            claims.Add(new Claim("JwtToken", jwtToken));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true, // mantém logado
                ExpiresUtc = DateTime.UtcNow.AddHours(4)
            });
        }

    }
}
