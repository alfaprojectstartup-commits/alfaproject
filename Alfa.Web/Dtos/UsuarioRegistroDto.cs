using System.ComponentModel.DataAnnotations;

namespace Alfa.Web.Dtos
{
    public class UsuarioRegistroDto
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        public required string Nome { get; set; }

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre {2} e {1} caracteres.")]
        [DataType(DataType.Password)]
        public required string Senha { get; set; }

        [Required(ErrorMessage = "Confirme a senha.")]
        [Compare("Senha", ErrorMessage = "A confirmação não confere com a senha.")]
        [DataType(DataType.Password)]
        public required string ConfirmarSenha { get; set; }

        public required int EmpresaId { get; set; }
    }
}
