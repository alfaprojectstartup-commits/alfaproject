using System;
using System.Collections.Generic;

namespace Alfa.Api.Dtos
{
    public class PaginaRespostaDto
    {
        public PaginaRespostaDto()
        {
        }

        public PaginaRespostaDto(int faseInstanciaId, int paginaInstanciaId, List<FieldResponseDto> campos)
        {
            FaseInstanciaId = faseInstanciaId;
            PaginaInstanciaId = paginaInstanciaId;
            Campos = campos;
        }

        public int FaseInstanciaId { get; set; }

        public int PaginaInstanciaId { get; set; }

        public List<FieldResponseDto> Campos { get; set; } = new List<FieldResponseDto>();
    }

    public class FieldResponseDto
    {
        public FieldResponseDto()
        {
        }

        public FieldResponseDto(int campoInstanciaId, string? valorTexto, decimal? valorNumero, DateTime? valorData, bool? valorBool)
        {
            CampoInstanciaId = campoInstanciaId;
            ValorTexto = valorTexto;
            ValorNumero = valorNumero;
            ValorData = valorData;
            ValorBool = valorBool;
        }

        public int CampoInstanciaId { get; set; }

        public string? ValorTexto { get; set; }

        public decimal? ValorNumero { get; set; }

        public DateTime? ValorData { get; set; }

        public bool? ValorBool { get; set; }
    }
}
