using System;
using System.Collections.Generic;
using System.Linq;
using Alfa.Api.Aplicacao.Interfaces;
using Alfa.Api.Dtos;
using Alfa.Api.Repositorios.Interfaces;

namespace Alfa.Api.Aplicacao
{
    public class ProcessoApp : IProcessoApp
    {
        private readonly IProcessoRepositorio _procRepo;
        private readonly IFaseRepositorio _faseRepo;
        private readonly IRespostaRepositorio _respostaRepo;

        public ProcessoApp(IProcessoRepositorio p, IFaseRepositorio f, IRespostaRepositorio r)
        {
            _procRepo = p;
            _faseRepo = f;
            _respostaRepo = r;
        }

        public Task<(int total, IEnumerable<ProcessoListItemDto> items)> Listar(int emp, int page, int pageSize, string? status)
            => _procRepo.ListarAsync(emp, page, pageSize, status);

        public Task<ProcessoDetalheDto?> Obter(int emp, int id)
            => _procRepo.ObterAsync(emp, id);

        public Task<int> Criar(int emp, ProcessoCriarDto dto)
            => _procRepo.CriarAsync(emp, dto.Titulo, dto.FaseModeloIds);

        public Task<IEnumerable<FaseInstanciaDto>> ListarFases(int empresaId, int processoId)
            => _faseRepo.ListarInstanciasAsync(empresaId, processoId);

        public async Task RegistrarResposta(int empresaId, int processoId, PaginaRespostaDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var processoDaFase = await _procRepo.ObterProcessoIdDaFaseAsync(empresaId, dto.FaseInstanciaId);
            if (!processoDaFase.HasValue || processoDaFase.Value != processoId)
            {
                throw new KeyNotFoundException("Fase não pertence ao processo informado.");
            }

            await _respostaRepo.SalvarPaginaAsync(empresaId, dto);
            await _faseRepo.RecalcularProgressoFaseAsync(empresaId, dto.FaseInstanciaId);
            await RecalcularProgressoProcesso(empresaId, processoId);
        }

        public Task<int?> ObterProcessoIdDaFase(int empresaId, int faseInstanciaId)
            => _procRepo.ObterProcessoIdDaFaseAsync(empresaId, faseInstanciaId);

        public async Task RecalcularProgressoProcesso(int empresaId, int processoId)
        {
            var fases = await _faseRepo.ListarInstanciasAsync(empresaId, processoId);
            var list = fases.ToList();
            var progresso = list.Count == 0 ? 0 : (int)Math.Round(list.Average(f => (double)f.PorcentagemProgresso));
            var status = progresso >= 100 ? "Concluído" : "Em Andamento";
            await _procRepo.AtualizarStatusEProgressoAsync(empresaId, processoId, status, progresso);
        }
    }
}
