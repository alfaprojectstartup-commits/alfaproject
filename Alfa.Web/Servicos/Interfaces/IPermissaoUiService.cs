namespace Alfa.Web.Servicos.Interfaces
{
    public interface IPermissaoUiService
    {
        Task<HashSet<string>> ObterPermissoesAsync(string token);
        bool PossuiPermissao(string codigo);
    }
}
