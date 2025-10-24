namespace Alfa.Web.Dtos
{
    public class ProcessoListaItemViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = "";
        public string Status { get; set; } = "";
        public int PorcentagemProgresso { get; set; }
    }

    public class PaginadoResultadoDto<T>
    {
        public int total { get; set; }
        public IEnumerable<T> items { get; set; } = Enumerable.Empty<T>();
    }
}
