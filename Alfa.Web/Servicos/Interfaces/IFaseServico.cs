using System.Net.Http;
using Alfa.Web.Dtos;
using Alfa.Web.Models;

namespace Alfa.Web.Servicos.Interfaces;

public interface IFaseServico
{
    Task<List<FaseModelosViewModel>?> ObterModelosAsync();
    Task<FaseModelosViewModel?> ObterModeloPorIdAsync(int id);
    Task<HttpResponseMessage> CriarModeloAsync(FaseTemplateInputDto payload);
    Task<HttpResponseMessage> AtualizarModeloAsync(int id, FaseTemplateInputDto payload);
}
