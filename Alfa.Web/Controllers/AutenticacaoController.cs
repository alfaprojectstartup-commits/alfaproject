using Microsoft.AspNetCore.Mvc;

namespace Alfa.Web.Controllers
{
    public class AuenticacaoController : Controller
    {
        [HttpGet("/login-mock")]
        public IActionResult LoginMock(int empresaId = 1, int usuarioId = 1, string nome = "Usuário Demo")
        {
            HttpContext.Session.SetInt32("EmpresaId", empresaId);
            HttpContext.Session.SetInt32("UsuarioId", usuarioId);
            HttpContext.Session.SetString("Nome", nome);
            return RedirectToAction("Index", "Processos");
        }

        [HttpGet("/logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("LoginMock");
        }
    }
}
