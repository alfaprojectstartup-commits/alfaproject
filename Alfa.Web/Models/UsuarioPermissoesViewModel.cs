using System.ComponentModel.DataAnnotations;

namespace Alfa.Web.Models
{
    public class UsuarioPermissoesViewModel
    {
        public int UsuarioId { get; set; }
        public string? UsuarioNome { get; set; }

        // Usado apenas para renderizar (não necessário no POST, mas útil)
        public List<PermissaoCheckboxViewModel> Permissoes { get; set; } = [];

        // No POST: ids das permissões marcadas (binding automático)
        [Display(Name = "Permissões Selecionadas")]
        public int[] PermissoesSelecionadas { get; set; } = [];
    }
}
