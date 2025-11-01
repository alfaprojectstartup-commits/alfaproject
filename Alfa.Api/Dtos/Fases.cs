using System.Collections.Generic;

namespace Alfa.Api.Dtos
{
    public class FaseModeloDto
    {
        public FaseModeloDto()
        {
        }

        public FaseModeloDto(int id, string titulo, int ordem, bool ativo)
        {
            Id = id;
            Titulo = titulo;
            Ordem = ordem;
            Ativo = ativo;
        }

        public int Id { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public int Ordem { get; set; }

        public bool Ativo { get; set; }
    }

    public class FaseInstanciaDto
    {
        public FaseInstanciaDto()
        {
        }

        public FaseInstanciaDto(
            int id,
            int faseModeloId,
            string titulo,
            int ordem,
            string status,
            int porcentagemProgresso,
            IEnumerable<PaginaInstanciaDto> paginas)
        {
            Id = id;
            FaseModeloId = faseModeloId;
            Titulo = titulo;
            Ordem = ordem;
            Status = status;
            PorcentagemProgresso = porcentagemProgresso;
            Paginas = paginas;
        }

        public int Id { get; set; }

        public int FaseModeloId { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public int Ordem { get; set; }

        public string Status { get; set; } = string.Empty;

        public int PorcentagemProgresso { get; set; }

        public IEnumerable<PaginaInstanciaDto> Paginas { get; set; } = new List<PaginaInstanciaDto>();
    }
}
