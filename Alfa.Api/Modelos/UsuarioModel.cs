namespace Alfa.Api.Modelos
{
    public class UsuarioModel
    {
        public required int Id { get; set; }
        public required string Nome { get; set; }
        public required string Email { get; set; }
        public required string SenhaHash { get; set; }
        public required int FuncaoId { get; set; }
        public required bool Ativo { get; set; }
    }
}
