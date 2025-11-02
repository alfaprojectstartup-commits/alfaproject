using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Alfa.Web.Dtos
{
    public class FaseTemplateInputDto
    {
        [Required]
        [MaxLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int Ordem { get; set; }

        public bool Ativo { get; set; } = true;

        public List<PaginaTemplateInputDto> Paginas { get; set; } = new();
    }

    public class PaginaTemplateInputDto
    {
        [Required]
        [MaxLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int Ordem { get; set; }

        public List<CampoTemplateInputDto> Campos { get; set; } = new();
    }

    public class CampoTemplateInputDto
    {
        [Required]
        [MaxLength(200)]
        public string NomeCampo { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Rotulo { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Tipo { get; set; } = string.Empty;

        public bool Obrigatorio { get; set; }

        [Range(0, int.MaxValue)]
        public int Ordem { get; set; }

        [MaxLength(500)]
        public string? Placeholder { get; set; }

        [MaxLength(200)]
        public string? Mascara { get; set; }

        [MaxLength(500)]
        public string? Ajuda { get; set; }

        public bool EhCatalogo { get; set; }

        public List<CampoOpcaoTemplateInputDto> Opcoes { get; set; } = new();
    }

    public class CampoOpcaoTemplateInputDto
    {
        [Required]
        [MaxLength(200)]
        public string Texto { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Valor { get; set; }

        [Range(0, int.MaxValue)]
        public int Ordem { get; set; }

        public bool Ativo { get; set; } = true;
    }
}
