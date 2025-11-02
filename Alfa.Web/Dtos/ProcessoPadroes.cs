using System.Collections.Generic;

namespace Alfa.Web.Dtos
{
    public class ProcessoPadraoModeloViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public List<int> FaseModeloIds { get; set; } = new();
    }

    public class ProcessoPadraoModeloInput
    {
        public string Titulo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public IList<int> FaseModeloIds { get; set; } = new List<int>();
    }
}
