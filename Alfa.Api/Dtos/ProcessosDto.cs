using System;
using System.Collections.Generic;
using System.Linq;

namespace Alfa.Api.Dtos
{
    public class ProcessoListItemDto
    {
        public ProcessoListItemDto()
        {
        }

        public ProcessoListItemDto(
            int id,
            string titulo,
            string status,
            int porcentagemProgresso,
            DateTime criadoEm,
            IEnumerable<string>? usuariosAlteracao = null)
        {
            Id = id;
            Titulo = titulo;
            Status = status;
            PorcentagemProgresso = porcentagemProgresso;
            CriadoEm = criadoEm;
            UsuariosAlteracao = usuariosAlteracao?.ToList() ?? new List<string>();
        }

        public int Id { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int PorcentagemProgresso { get; set; }

        public DateTime CriadoEm { get; set; }

        public List<string> UsuariosAlteracao { get; set; } = new();
    }

    public class ProcessoDetalheDto
    {
        public ProcessoDetalheDto()
        {
        }

        public ProcessoDetalheDto(
            int id,
            string titulo,
            string status,
            int porcentagemProgresso,
            DateTime criadoEm,
            IEnumerable<FaseInstanciaDto> fases)
        {
            Id = id;
            Titulo = titulo;
            Status = status;
            PorcentagemProgresso = porcentagemProgresso;
            CriadoEm = criadoEm;
            Fases = fases;
        }

        public int Id { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int PorcentagemProgresso { get; set; }

        public DateTime CriadoEm { get; set; }

        public IEnumerable<FaseInstanciaDto> Fases { get; set; } = new List<FaseInstanciaDto>();
    }

    public class ProcessoCriarDto
    {
        public ProcessoCriarDto()
        {
        }

        public ProcessoCriarDto(string titulo, int[] faseModeloIds)
        {
            Titulo = titulo;
            FaseModeloIds = faseModeloIds;
        }

        public string Titulo { get; set; } = string.Empty;

        public int[] FaseModeloIds { get; set; } = Array.Empty<int>();
    }

    public class ProcessoUpdateDto
    {
        public ProcessoUpdateDto()
        {
        }

        public ProcessoUpdateDto(string? titulo, string? status)
        {
            Titulo = titulo;
            Status = status;
        }

        public string? Titulo { get; set; }

        public string? Status { get; set; }
    }
}
