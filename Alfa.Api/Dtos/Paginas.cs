using System.Collections.Generic;

namespace Alfa.Api.Dtos
{
    public class PaginaModeloDto
    {
        public int Id { get; set; }

        public int FaseModeloId { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public int Ordem { get; set; }
    }

    public class PaginaInstanciaDto
    {
        public int Id { get; set; }

        public int PaginaModeloId { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public int Ordem { get; set; }

        public bool Concluida { get; set; }

        public IEnumerable<CampoInstanciaDto> Campos { get; set; } = new List<CampoInstanciaDto>();
    }
}
