using System;
using System.Collections.Generic;

namespace Alfa.Api.Dtos;

public record ProcessoListItemDto(
        int Id,
        string Titulo,
        string Status,
        int PorcentagemProgresso
    );

public record ProcessoDetalheDto(
    int Id,
    string Titulo,
    string Status,
    int PorcentagemProgresso,
    DateTime CriadoEm,
    IEnumerable<FaseInstanciaDto> Fases
);

public record ProcessoCriarDto(string Titulo, int[] FaseModeloIds);

public record ProcessoUpdateDto(string? Titulo, string? Status);
