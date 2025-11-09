using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

    public class ProcessoPadraoModeloDto
    {
        public ProcessoPadraoModeloDto()
        {
        }

        public ProcessoPadraoModeloDto(int id, string titulo, string? descricao, IReadOnlyCollection<int> faseModeloIds)
        {
            Id = id;
            Titulo = titulo;
            Descricao = descricao;
            FaseModeloIds = faseModeloIds?.ToArray() ?? Array.Empty<int>();
        }

        public int Id { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        public IReadOnlyCollection<int> FaseModeloIds { get; set; } = Array.Empty<int>();
    }

    public class ProcessoPadraoModeloInputDto
    {
        [Required]
        [MaxLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descricao { get; set; }

        [MinLength(1, ErrorMessage = "Selecione ao menos uma fase para o padrão.")]
        public IList<int> FaseModeloIds { get; set; } = new List<int>();
    }

    public class ProcessoHistoricoDto
    {
        public int Id { get; set; }

        public int ProcessoId { get; set; }

        public int? UsuarioId { get; set; }

        public string UsuarioNome { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; }
    }

    public class ProcessoStatusAtualizarDto
    {
        [Required]
        [MaxLength(200)]
        public string Status { get; set; } = string.Empty;

        public int? UsuarioId { get; set; }

        [MaxLength(250)]
        public string? UsuarioNome { get; set; }
    }
}
