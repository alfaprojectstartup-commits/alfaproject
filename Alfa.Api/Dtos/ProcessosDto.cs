namespace Alfa.Api.Dtos;

public record ProcessoListItemDto(int Id, string Titulo, string Status, int ProgressoPct, DateTime CriadoEm);
public record ProcessoDetalheDto(int Id, string Titulo, string Status, int ProgressoPct, IEnumerable<FaseInstanceDto> Fases);
public record ProcessoCreateDto(string Titulo, int[] FasesTemplateIds);
public record ProcessoUpdateDto(string? Titulo, string? Status);