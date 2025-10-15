namespace Alfa.Api.Dtos;

public record ProcessoListaDto(int Id, string Titulo, string Status, int ProgressoPct, DateTime CriadoEm);
public record ProcessoCriarDto(string Titulo, int EmpresaId);
public record ProcessoAtualizarTituloDto(string Titulo);

public record Paginado<T>(IEnumerable<T> Itens, int Total, int Pagina, int Tamanho);