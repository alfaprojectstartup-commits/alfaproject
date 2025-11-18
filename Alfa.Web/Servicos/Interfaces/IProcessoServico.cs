using System.Net.Http;
using Alfa.Web.Dtos;
using Alfa.Web.Models;

namespace Alfa.Web.Servicos.Interfaces;

public interface IProcessoServico
{
    Task<PaginadoResultadoDto<ProcessoListaItemViewModel>?> ObterProcessosAsync(int page, int pageSize, string? status);
    Task<ProcessoDetalheViewModel?> ObterProcessoAsync(int id);
    Task<HttpResponseMessage> CriarProcessoAsync(string titulo, int[] fasesTemplateIds);
    Task<HttpResponseMessage> RegistrarRespostasAsync(int processoId, PaginaRespostaInput payload, string? preenchimentoToken = null);
    Task<List<ProcessoPadraoModeloViewModel>?> ObterProcessoPadraoModelosAsync();
    Task<HttpResponseMessage> CriarProcessoPadraoAsync(ProcessoPadraoModeloInput payload);
    Task<HttpResponseMessage> AtualizarStatusAsync(int id, ProcessoStatusAtualizarInput payload);
    Task<List<ProcessoHistoricoViewModel>?> ObterHistoricoAsync(int processoId);
}
