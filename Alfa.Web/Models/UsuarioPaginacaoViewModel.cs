namespace Alfa.Web.Models
{
    public class UsuarioPaginacaoViewModel<T>
    {
        public IEnumerable<T> Itens { get; set; } = [];
        public int PaginaAtual { get; set; }
        public int TotalPaginas { get; set; }
    }
}
