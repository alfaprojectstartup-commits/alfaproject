﻿using Alfa.Api.Dtos;

namespace Alfa.Api.Aplicacao.Interfaces
{
    public interface IProcessoApp
    {
        Task<(int total, IEnumerable<ProcessoListItemDto> items)> Listar(int empresaId, int page, int pageSize, string? status);
        Task<int> Criar(int empresaId, ProcessoCriarDto dto);
        Task<ProcessoDetalheDto?> Obter(int empresaId, int id);
        Task<IEnumerable<FaseInstanciaDto>> ListarFases(int empresaId, int processoId);
        Task RegistrarResposta(int empresaId, int processoId, PaginaRespostaDto dto);
        Task<int?> ObterProcessoIdDaFase(int empresaId, int faseInstanciaId);
        Task RecalcularProgressoProcesso(int empresaId, int processoId);
    }
}
