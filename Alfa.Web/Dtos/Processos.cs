using System;
using System.Collections.Generic;
using System.Linq;

namespace Alfa.Web.Dtos
{
    public class ProcessoListaItemViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = "";
        public string Status { get; set; } = "";
        public int PorcentagemProgresso { get; set; }
        public DateTime CriadoEm { get; set; }
        public List<string> UsuariosAlteracao { get; set; } = new();
    }

    public class PaginadoResultadoDto<T>
    {
        public int total { get; set; }
        public IEnumerable<T> items { get; set; } = Enumerable.Empty<T>();
    }

    public class ProcessoDetalheViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = "";
        public string Status { get; set; } = "";
        public int PorcentagemProgresso { get; set; }
        public DateTime CriadoEm { get; set; }
        public IList<FaseInstanciaViewModel> Fases { get; set; } = new List<FaseInstanciaViewModel>();
    }

    public class FaseInstanciaViewModel
    {
        public int Id { get; set; }
        public int FaseModeloId { get; set; }
        public string Titulo { get; set; } = "";
        public int Ordem { get; set; }
        public string Status { get; set; } = "";
        public int PorcentagemProgresso { get; set; }
        public IList<PaginaInstanciaViewModel> Paginas { get; set; } = new List<PaginaInstanciaViewModel>();
    }

    public class PaginaInstanciaViewModel
    {
        public int Id { get; set; }
        public int PaginaModeloId { get; set; }
        public string Titulo { get; set; } = "";
        public int Ordem { get; set; }
        public bool Concluida { get; set; }
        public IList<CampoInstanciaViewModel> Campos { get; set; } = new List<CampoInstanciaViewModel>();
    }

    public class CampoInstanciaViewModel
    {
        public int Id { get; set; }
        public int CampoModeloId { get; set; }
        public string NomeCampo { get; set; } = "";
        public string Rotulo { get; set; } = "";
        public string Tipo { get; set; } = "";
        public bool Obrigatorio { get; set; }
        public int Ordem { get; set; }
        public string? Placeholder { get; set; }
        public string? Mascara { get; set; }
        public string? Ajuda { get; set; }
        public string? ValorTexto { get; set; }
        public decimal? ValorNumero { get; set; }
        public DateTime? ValorData { get; set; }
        public bool? ValorBool { get; set; }
        public IList<CampoOpcaoViewModel> Opcoes { get; set; } = new List<CampoOpcaoViewModel>();
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

    public class PaginaRespostaInput
    {
        public int FaseInstanciaId { get; set; }
        public int PaginaInstanciaId { get; set; }
        public IList<FieldResponseInput> Campos { get; set; } = new List<FieldResponseInput>();
    }

    public class FieldResponseInput
    {
        public int CampoInstanciaId { get; set; }
        public string? ValorTexto { get; set; }
        public decimal? ValorNumero { get; set; }
        public DateTime? ValorData { get; set; }
        public bool? ValorBool { get; set; }
    }
}
