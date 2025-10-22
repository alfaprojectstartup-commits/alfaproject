namespace Alfa.Api.Dtos.Comuns
{
    public record PaginadoResultDto<T>(int total, IEnumerable<T> items);
}
