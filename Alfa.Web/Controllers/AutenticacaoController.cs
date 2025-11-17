using Alfa.Web.Dtos;
using Alfa.Web.Servicos.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Alfa.Web.Controllers
{
    public class AutenticacaoController : Controller
    {
        private readonly IAutenticacaoServico _autenticacaoServico;
        private readonly IPermissaoUiServico _permissaoUiServico;

        public AutenticacaoController(IAutenticacaoServico autenticacaoServico, IPermissaoUiServico permissaoUiServico)
        {
            _autenticacaoServico = autenticacaoServico;
            _permissaoUiServico = permissaoUiServico;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UsuarioLoginDto model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var autenticacao = await _autenticacaoServico.LoginAsync(model);
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

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            Response.Cookies.Delete("JwtToken");

            return RedirectToAction("Login", "Autenticacao");
        }

        //[HttpGet("/login-mock")]
        //public IActionResult LoginMock(int empresaId = 1, int usuarioId = 1, string nome = "Usuário Demo")
        //{
        //    HttpContext.Session.SetInt32("EmpresaId", empresaId);
        //    HttpContext.Session.SetInt32("UsuarioId", usuarioId);
        //    HttpContext.Session.SetString("Nome", nome);
        //    return RedirectToAction("Index", "Processos");
        //}

        //[HttpGet("/logout")]
        //public IActionResult Logout()
        //{
        //    HttpContext.Session.Clear();
        //    return RedirectToAction("LoginMock");
        //}

        private void ArmazenarTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(8) // comentar somente em local, para o token expirar junto com a sessão
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

            var nomeClaim = token.Claims.FirstOrDefault(c =>
                c.Type.Equals("nome", StringComparison.OrdinalIgnoreCase) ||
                c.Type.Equals(ClaimTypes.Name, StringComparison.OrdinalIgnoreCase));

            if (nomeClaim != null)
            {
                claims.Add(new Claim(ClaimTypes.Name, nomeClaim.Value));
            }

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
