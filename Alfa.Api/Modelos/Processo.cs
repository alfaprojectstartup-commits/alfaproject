namespace Alfa.Api.Modelos;
public class Processo
{
    public int Id { get; set; }
    public string Titulo { get; set; } = default!;
    public string Status { get; set; } = "EmAndamento";
    public int ProgressoPct { get; set; }
    public int EmpresaId { get; set; }
    public DateTime CriadoEm { get; set; }
}