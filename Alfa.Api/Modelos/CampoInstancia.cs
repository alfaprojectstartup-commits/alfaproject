namespace Alfa.Api.Modelos;

public class CampoInstancia
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int PaginaInstanciaId { get; set; }
    public int CampoModeloId { get; set; }
    public string NomeCampo { get; set; } = default!;
    public string Rotulo { get; set; } = default!;
    public string Tipo { get; set; } = default!;
    public bool Obrigatorio { get; set; }
    public int Ordem { get; set; }
    public string? Placeholder { get; set; }
    public string? Mascara { get; set; }
    public string? Ajuda { get; set; }
    public string? ValorTexto { get; set; }
    public decimal? ValorNumero { get; set; }
    public DateTime? ValorData { get; set; }
    public bool? ValorBool { get; set; }
}
