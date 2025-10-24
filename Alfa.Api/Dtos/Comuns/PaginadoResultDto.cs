namespace Alfa.Api.Dtos.Comuns
{
    public record PaginadoResultadoDto<T>(int total, IEnumerable<T> items);
}
