namespace Alfa.Api.Modelos;

public class FaseInstancia
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int ProcessoId { get; set; }
    public int FaseModeloId { get; set; }
    public string Titulo { get; set; } = default!;
    public int Ordem { get; set; }
    public int StatusId { get; set; }
    public string Status { get; set; } = default!;
    public int PorcentagemProgresso { get; set; }
    public DateTime CriadoEm { get; set; }
}
