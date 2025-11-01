using System;
using System.Collections.Generic;

namespace Alfa.Api.Dtos
{
    public class CampoModeloDto
    {
        public CampoModeloDto()
        {
        }

        public CampoModeloDto(
            int id,
            int paginaModeloId,
            string nomeCampo,
            string rotulo,
            string tipo,
            bool obrigatorio,
            int ordem,
            string? placeholder,
            string? mascara,
            string? ajuda)
        {
            Id = id;
            PaginaModeloId = paginaModeloId;
            NomeCampo = nomeCampo;
            Rotulo = rotulo;
            Tipo = tipo;
            Obrigatorio = obrigatorio;
            Ordem = ordem;
            Placeholder = placeholder;
            Mascara = mascara;
            Ajuda = ajuda;
        }

        public int Id { get; set; }

        public int PaginaModeloId { get; set; }

        public string NomeCampo { get; set; } = string.Empty;

        public string Rotulo { get; set; } = string.Empty;

        public string Tipo { get; set; } = string.Empty;

        public bool Obrigatorio { get; set; }

        public int Ordem { get; set; }

        public string? Placeholder { get; set; }

        public string? Mascara { get; set; }

        public string? Ajuda { get; set; }
    }

    public class CampoInstanciaDto
    {
        public CampoInstanciaDto()
        {
        }

        public CampoInstanciaDto(
            int id,
            int campoModeloId,
            string nomeCampo,
            string rotulo,
            string tipo,
            bool obrigatorio,
            int ordem,
            string? placeholder,
            string? mascara,
            string? ajuda,
            string? valorTexto,
            decimal? valorNumero,
            DateTime? valorData,
            bool? valorBool)
        {
            Id = id;
            CampoModeloId = campoModeloId;
            NomeCampo = nomeCampo;
            Rotulo = rotulo;
            Tipo = tipo;
            Obrigatorio = obrigatorio;
            Ordem = ordem;
            Placeholder = placeholder;
            Mascara = mascara;
            Ajuda = ajuda;
            ValorTexto = valorTexto;
            ValorNumero = valorNumero;
            ValorData = valorData;
            ValorBool = valorBool;
        }

        public int Id { get; set; }

        public int CampoModeloId { get; set; }

        public string NomeCampo { get; set; } = string.Empty;

        public string Rotulo { get; set; } = string.Empty;

        public string Tipo { get; set; } = string.Empty;

        public bool Obrigatorio { get; set; }

        public int Ordem { get; set; }

        public string? Placeholder { get; set; }

        public string? Mascara { get; set; }

        public string? Ajuda { get; set; }

        public string? ValorTexto { get; set; }

        public decimal? ValorNumero { get; set; }

        public DateTime? ValorData { get; set; }

        public bool? ValorBool { get; set; }
    }

    public class CampoOpcaoDto
    {
        public CampoOpcaoDto()
        {
        }

        public CampoOpcaoDto(int id, int campoModeloId, string texto, string? valor, int ordem, bool ativo)
        {
            Id = id;
            CampoModeloId = campoModeloId;
            Texto = texto;
            Valor = valor;
            Ordem = ordem;
            Ativo = ativo;
        }

        public int Id { get; set; }

        public int CampoModeloId { get; set; }

        public string Texto { get; set; } = string.Empty;

        public string? Valor { get; set; }

        public int Ordem { get; set; }

        public bool Ativo { get; set; }
    }
}
