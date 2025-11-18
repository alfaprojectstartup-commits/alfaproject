using Alfa.Web.Models;

namespace Alfa.Web.Servicos.Interfaces
{
    public interface IPermissaoUiServico
    {
        Task<IEnumerable<PermissaoViewModel>> ListarPermissoesSistemaAsync();
        Task<HashSet<string>> ObterPermissoesAsync(string token);
        bool PossuiPermissao(string codigo);
    }
}
