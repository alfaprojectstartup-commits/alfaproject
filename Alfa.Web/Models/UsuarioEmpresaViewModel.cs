namespace Alfa.Web.Models
{
    public class UsuarioEmpresaViewModel
    {
        public int Id { get; set; }
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public int EmpresaId { get; set; }
        public bool Ativo { get; set; }
    }
}
