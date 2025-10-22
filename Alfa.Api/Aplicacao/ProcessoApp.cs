using Alfa.Api.Aplicacao.Interfaces;
using Alfa.Api.Dtos;
using Alfa.Api.Repositorios.Interfaces;

namespace Alfa.Api.Aplicacao
{
    public class ProcessoApp : IProcessoApp
    {
        private readonly IProcessoRepositorio _procRepo;
        private readonly IFaseRepositorio _faseRepo;

        public ProcessoApp(IProcessoRepositorio p, IFaseRepositorio f) { _procRepo = p; _faseRepo = f; }

        public Task<(int total, IEnumerable<ProcessoListItemDto> items)> Listar(int emp, int page, int pageSize, string? status)
            => _procRepo.ListarAsync(emp, page, pageSize, status);

        public Task<ProcessoDetalheDto?> Obter(int emp, int id)
            => _procRepo.ObterAsync(emp, id);

        public Task<int> Criar(int emp, ProcessoCreateDto dto)
            => _procRepo.CriarAsync(emp, dto.Titulo, dto.FasesTemplateIds);

        public async Task RecalcularProgressoProcesso(int empresaId, int processoId)
        {
            // média das fases
            var fases = await _faseRepo.ListarInstanciasAsync(empresaId, processoId);
            var list = fases.ToList();
            var progresso = list.Count == 0 ? 0 : (int)Math.Round(list.Average(f => (double)f.ProgressoPct));
            var status = progresso >= 100 ? "Concluido" : "EmAndamento";
            await _procRepo.AtualizarStatusEProgressoAsync(empresaId, processoId, status, progresso);
        }
    }
}
