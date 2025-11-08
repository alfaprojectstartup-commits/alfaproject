using Alfa.Web.Dtos;

namespace Alfa.Web.Models;

public class PaginaExternaViewModel
{
    public int ProcessoId { get; set; }
    public string ProcessoTitulo { get; set; } = string.Empty;
    public int FaseId { get; set; }
    public string FaseTitulo { get; set; } = string.Empty;
    public PaginaInstanciaViewModel Pagina { get; set; } = new();
}
