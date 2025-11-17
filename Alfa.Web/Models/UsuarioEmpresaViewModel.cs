using System.ComponentModel.DataAnnotations;

namespace Alfa.Web.Models
{
    public class UsuarioEmpresaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O campo Nome é obrigatório.")]
        public string? Nome { get; set; }

        [Required(ErrorMessage = "O campo Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Digite um email válido.")]
        public string? Email { get; set; }

        public int EmpresaId { get; set; }
        public bool Ativo { get; set; }
    }
}
