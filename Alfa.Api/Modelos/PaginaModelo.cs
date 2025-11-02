namespace Alfa.Api.Modelos;

public class PaginaModelo
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int FaseModeloId { get; set; }
    public string Titulo { get; set; } = default!;
    public int Ordem { get; set; }
}
