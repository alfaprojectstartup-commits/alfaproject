using System.Collections.Generic;

namespace Alfa.Api.Dtos.Comuns
{
    public class PaginadoResultadoDto<T>
    {
        public PaginadoResultadoDto()
        {
        }

        public PaginadoResultadoDto(int total, IEnumerable<T> items)
        {
            Total = total;
            Items = items;
        }

        public int Total { get; set; }

        public IEnumerable<T> Items { get; set; } = new List<T>();
    }
}
