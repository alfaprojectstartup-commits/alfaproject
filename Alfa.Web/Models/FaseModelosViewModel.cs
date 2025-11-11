using System.Collections.Generic;

namespace Alfa.Web.Models
{
    public class FaseModelosViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public int Ordem { get; set; }
        public bool Ativo { get; set; }
        public List<PaginaModeloViewModel> Paginas { get; set; } = new();
        public string Token { get; set; } = string.Empty;
    }

    public class PaginaModeloViewModel
    {
        public int Id { get; set; }
        public int FaseModeloId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public int Ordem { get; set; }
        public List<CampoModeloViewModel> Campos { get; set; } = new();
    }

    public class CampoModeloViewModel
    {
        public int Id { get; set; }
        public int PaginaModeloId { get; set; }
        public string NomeCampo { get; set; } = string.Empty;
        public string Rotulo { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public bool Obrigatorio { get; set; }
        public int Ordem { get; set; }
        public string? Placeholder { get; set; }
        public string? Mascara { get; set; }
        public string? Ajuda { get; set; }
        public List<CampoOpcaoViewModel> Opcoes { get; set; } = new();
    }

    public class CampoOpcaoViewModel
    {
        public int Id { get; set; }
        public int CampoModeloId { get; set; }
        public string Texto { get; set; } = string.Empty;
        public string? Valor { get; set; }
        public int Ordem { get; set; }
        public bool Ativo { get; set; }
    }
}
