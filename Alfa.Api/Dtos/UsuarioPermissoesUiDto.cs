namespace Alfa.Api.Dtos
{
    public class UsuarioPermissoesUiDto
    {
        public int UsuarioId { get; set; }
        public List<string> Permissoes { get; set; } = [];   
    }
}
