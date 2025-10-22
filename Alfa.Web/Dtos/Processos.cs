namespace Alfa.Web.Dtos
{
    public record PaginadoResultDto<T>(int total, IEnumerable<T> items);
    public record ProcessoListItemVm(int Id, string Titulo, string Status, int ProgressoPct, DateTime CriadoEm);
}
