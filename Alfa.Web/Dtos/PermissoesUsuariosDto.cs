namespace Alfa.Web.Dtos
{
    public class PermissoesUsuariosDto
    {
        public int UsuarioId { get; set; }
        public List<string> Permissoes { get; set; } = [];
    }
}
