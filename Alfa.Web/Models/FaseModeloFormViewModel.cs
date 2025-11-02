using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Alfa.Web.Models
{
    public class FaseModeloFormViewModel
    {
        public int? Id { get; set; }

        [Required]
        [Display(Name = "Título da fase")]
        public string Titulo { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        [Display(Name = "Ordem")]
        public int Ordem { get; set; }

        [Display(Name = "Ativa")]
        public bool Ativo { get; set; } = true;

        public List<PaginaModeloFormViewModel> Paginas { get; set; } = new();

        public List<CampoModeloViewModel> CatalogoCampos { get; set; } = new();
    }

    public class PaginaModeloFormViewModel
    {
        public PaginaModeloFormViewModel()
        {
        }

        public PaginaModeloFormViewModel(PaginaModeloViewModel origem)
        {
            Id = origem.Id;
            Titulo = origem.Titulo;
            Ordem = origem.Ordem;

            foreach (var campo in origem.Campos ?? new List<CampoModeloViewModel>())
            {
                Campos.Add(new CampoModeloFormViewModel(campo));
            }
        }

        public int? Id { get; set; }

        [Required]
        [Display(Name = "Título da página")]
        public string Titulo { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        [Display(Name = "Ordem")]
        public int Ordem { get; set; }

        public List<CampoModeloFormViewModel> Campos { get; set; } = new();
    }

    public class CampoModeloFormViewModel
    {
        public CampoModeloFormViewModel()
        {
        }

        public CampoModeloFormViewModel(CampoModeloViewModel origem)
        {
            Id = origem.Id;
            NomeCampo = origem.NomeCampo;
            Rotulo = origem.Rotulo;
            Tipo = origem.Tipo;
            Obrigatorio = origem.Obrigatorio;
            Ordem = origem.Ordem;
            Placeholder = origem.Placeholder;
            Mascara = origem.Mascara;
            Ajuda = origem.Ajuda;

            foreach (var opcao in origem.Opcoes ?? new List<CampoOpcaoViewModel>())
            {
                Opcoes.Add(new CampoOpcaoFormViewModel(opcao));
            }
        }

        public int? Id { get; set; }

        [Required]
        [Display(Name = "Nome interno")]
        public string NomeCampo { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Rótulo")]
        public string Rotulo { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Tipo")]
        public string Tipo { get; set; } = string.Empty;

        [Display(Name = "Obrigatório")]
        public bool Obrigatorio { get; set; }

        [Display(Name = "Modelo de campo")]
        public int? ModeloCatalogoId { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "Ordem")]
        public int Ordem { get; set; }

        [Display(Name = "Placeholder")]
        public string? Placeholder { get; set; }

        [Display(Name = "Máscara")]
        public string? Mascara { get; set; }

        [Display(Name = "Ajuda")]
        public string? Ajuda { get; set; }

        public List<CampoOpcaoFormViewModel> Opcoes { get; set; } = new();
    }

    public class CampoOpcaoFormViewModel
    {
        public CampoOpcaoFormViewModel()
        {
        }

        public CampoOpcaoFormViewModel(CampoOpcaoViewModel origem)
        {
            Id = origem.Id;
            Texto = origem.Texto;
            Valor = origem.Valor;
            Ordem = origem.Ordem;
            Ativo = origem.Ativo;
        }

        public int? Id { get; set; }

        [Required]
        [Display(Name = "Texto")]
        public string Texto { get; set; } = string.Empty;

        [Display(Name = "Valor (opcional)")]
        public string? Valor { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "Ordem")]
        public int Ordem { get; set; }

        [Display(Name = "Ativa")]
        public bool Ativo { get; set; } = true;
    }
}
