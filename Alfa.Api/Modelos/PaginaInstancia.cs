namespace Alfa.Api.Modelos;

public class PaginaInstancia
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int FaseInstanciaId { get; set; }
    public int PaginaModeloId { get; set; }
    public string Titulo { get; set; } = default!;
    public int Ordem { get; set; }
    public bool Concluida { get; set; }
}
