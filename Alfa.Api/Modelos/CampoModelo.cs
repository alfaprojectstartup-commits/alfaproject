namespace Alfa.Api.Modelos;

public class CampoModelo
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int PaginaModeloId { get; set; }
    public string NomeCampo { get; set; } = default!;
    public string Rotulo { get; set; } = default!;
    public string Tipo { get; set; } = default!;
    public bool Obrigatorio { get; set; }
    public string? Placeholder { get; set; }
    public string? Mascara { get; set; }
    public string? Ajuda { get; set; }
    public int Ordem { get; set; }
}
