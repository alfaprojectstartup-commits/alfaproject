namespace Alfa.Api.Dtos
{
    public class PermissoesUsuarioDto
    {
        public int UsuarioId { get; set; }
        public List<string> Permissoes { get; set; } = [];   
    }
}
