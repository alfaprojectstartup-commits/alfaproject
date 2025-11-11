namespace Alfa.Api.Dtos
{
    public class UsuarioRegistroDto
    {
        public required string Nome { get; set; }
        public required string Email { get; set; }
        public required string Senha { get; set; }
        public required int EmpresaId { get; set; }
    }
}
