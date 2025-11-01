namespace Alfa.Api.Modelos;

public class FaseModelo
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Titulo { get; set; } = default!;
    public int Ordem { get; set; }
    public bool Ativo { get; set; }
}
