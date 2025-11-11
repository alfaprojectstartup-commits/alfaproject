namespace Alfa.Api.Dtos
{
    public class UsuarioEmpresaDto
    {
        public required int Id { get; set; }
        public required string Nome { get; set; }
        public required string Email { get; set; }
        public required int EmpresaId { get; set; }
        public required bool Ativo { get; set; }
    }
}
