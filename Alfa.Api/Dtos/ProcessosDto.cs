namespace Alfa.Api.Dtos;
public record ProcessoListItemDto(
        int Id,
        string Titulo,
        string Status,
        int PorcentagemProgresso
    );

public record PaginadoResultDto<T>(int total, IEnumerable<T> items);
public record ProcessoDetalheDto(int Id, string Titulo, string Status, int PorcentagemProgresso, IEnumerable<FasesDto> Fases);
public record ProcessoCriarDto(string Titulo, int[] FasesTemplateIds);
public record ProcessoUpdateDto(string? Titulo, string? Status);