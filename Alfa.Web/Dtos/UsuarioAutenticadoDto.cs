namespace Alfa.Web.Dtos
{
    public class UsuarioAutenticadoDto
    {
        public required string Email { get; set; }
        public required string Token { get; set; }
    }
}
