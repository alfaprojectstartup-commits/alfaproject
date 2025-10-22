using Alfa.Api.Dtos;

namespace Alfa.Api.Repositorios.Interfaces
{
    public interface IPaginaRepositorio
    {
        Task<IEnumerable<PaginaTemplateDto>> ListarTemplatesPorFaseTemplateAsync(int empresaId, int faseTemplateId);
        Task<IEnumerable<PaginaTemplateDto>> ListarTemplatesPorFaseInstanceAsync(int empresaId, int faseInstanceId);
    }
}
