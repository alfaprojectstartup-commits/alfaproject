namespace Alfa.Web.Models
{
    public class UsuarioPermissaoViewModel
    {
        public int UsuarioId { get; set; }
        public int PermissaoId { get; set; }
        public DateTimeOffset ConcedidoEm { get; set; }
        public int ConcedidoPor { get; set; }
    }
}
