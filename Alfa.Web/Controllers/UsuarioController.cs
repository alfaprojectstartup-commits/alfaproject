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
        private readonly IPermissaoUiServico _permissaoUiServico;

        public UsuarioController(IUsuarioServico usuarioServico, IPermissaoUiServico permissaoUiServico)
        {
            _usuarioServico = usuarioServico;
            _permissaoUiServico = permissaoUiServico;
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

        public async Task<IActionResult> Editar(int id)
        {
            var usuario = await _usuarioServico.ObterUsuarioPorIdAsync(id);
            if (usuario == null)
            {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(UsuarioEmpresaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Editar", model);
            }

            var usuarioAtualizado = await _usuarioServico.AtualizarDadosUsuarioAsync(model);
            if (!usuarioAtualizado.Success)
            {
                TempData["erro"] = "Erro ao atualizar dados do usuário.";
                return View("Editar", model);
            }

            TempData["ok"] = "Usuário atualizado com sucesso.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Permissoes(int id)
        {
            var empresaIdClaim = User.FindFirst("empresaId")?.Value;
            if (!int.TryParse(empresaIdClaim, out var empresaId))
            {
                return Forbid();
            }

            var usuario = await _usuarioServico.ObterUsuarioPorIdAsync(id);
            if (usuario == null || usuario.EmpresaId != empresaId)
            {
                return NotFound();
            }

            // carregar todas permissões do sistema
            var permissoesSistema = (await _permissaoUiServico.ListarPermissoesSistemaAsync())
                .OrderBy(permissaoSistema => permissaoSistema.Nome)
                .ToList();

            if (permissoesSistema is null || permissoesSistema.Count == 0)
            {
                TempData["ok"] = "Erro ao encontrar as permissões do sistema.";
                return NotFound("Erro ao encontrar as permissões do sistema.");
            }

            // carregar ids das permissões do usuário
            var permissoesUsuarioIds = (await _usuarioServico.ObterPermissoesUsuarioAsync(id))
                .Select(permissao => permissao.PermissaoId)
                .ToList();

            var viewModel = new UsuarioPermissoesViewModel
            {
                UsuarioId = usuario.Id,
                UsuarioNome = usuario.Nome,
                Permissoes = permissoesSistema.Select(permissao => new PermissaoCheckboxViewModel
                {
                    Id = permissao.Id,
                    Nome = permissao.Nome,
                    Codigo = permissao.Codigo,
                    Selected = permissoesUsuarioIds.Contains(permissao.Id)
                }).ToList()
            };

            return View(viewModel);
        }

        
        //ONDE PAREI:
        // Criei a tela de visualização das permissões
        // Mas falta criar a funcionalidade de salvar as permissões alteradas

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Permissoes(UsuarioPermissoesViewModel model)
        //{
        //    // validar model básico
        //    if (model == null) return BadRequest();

        //    // opcional: checar permissão/empresa do usuário
        //    var empresaIdClaim = User.FindFirst("empresaId")?.Value;
        //    if (!int.TryParse(empresaIdClaim, out var empresaId))
        //        return Forbid();

        //    var usuario = await _context.Usuarios
        //        .FirstOrDefaultAsync(u => u.Id == model.UsuarioId && u.EmpresaId == empresaId);

        //    if (usuario == null) return NotFound();

        //    // ids marcados no POST
        //    var selecionados = model.PermissoesSelecionadas ?? new int[0];

        //    // carregar ids atuais no banco
        //    var atuais = await _context.UsuariosPermissoes
        //        .Where(up => up.UsuarioId == model.UsuarioId)
        //        .Select(up => up.PermissaoId)
        //        .ToListAsync();

        //    // calcular o que inserir e o que remover
        //    var idsParaInserir = selecionados.Except(atuais).ToList(); // novos selects
        //    var idsParaRemover = atuais.Except(selecionados).ToList(); // desmarcados

        //    using var transaction = await _context.Database.BeginTransactionAsync();
        //    try
        //    {
        //        // inserir novos
        //        if (idsParaInserir.Any())
        //        {
        //            var now = DateTimeOffset.UtcNow;
        //            var usuarioId = model.UsuarioId;
        //            var listToAdd = idsParaInserir.Select(pid => new UsuariosPermissoes
        //            {
        //                UsuarioId = usuarioId,
        //                PermissaoId = pid,
        //                ConcedidoEm = now,
        //                ConcedidoPor = /* opcional: pegar id do usuário atual */ null
        //            }).ToList();

        //            _context.UsuariosPermissoes.AddRange(listToAdd);
        //        }

        //        // remover antigos
        //        if (idsParaRemover.Any())
        //        {
        //            var removals = await _context.UsuariosPermissoes
        //                .Where(up => up.UsuarioId == model.UsuarioId && idsParaRemover.Contains(up.PermissaoId))
        //                .ToListAsync();

        //            _context.UsuariosPermissoes.RemoveRange(removals);
        //        }

        //        await _context.SaveChangesAsync();
        //        await transaction.CommitAsync();

        //        TempData["ok"] = "Permissões atualizadas com sucesso.";
        //        return RedirectToAction("Index");
        //    }
        //    catch (Exception ex)
        //    {
        //        await transaction.RollbackAsync();
        //        // log do erro (ex) aqui
        //        TempData["erro"] = "Erro ao atualizar permissões.";
        //        // se quiser, re-montar model.Permissoes para reexibir a view com erros
        //        return RedirectToAction("Permissoes", new { id = model.UsuarioId });
        //    }
        //}
    }
}
