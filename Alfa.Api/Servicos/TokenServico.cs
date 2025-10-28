using Alfa.Api.Dtos;
using Alfa.Api.Modelos;
using Alfa.Api.Servicos.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Alfa.Api.Servicos
{
    public class TokenServico : ITokenServico
    {
        private readonly TokenJwtDto _jwtConfiguracao;

        public TokenServico(TokenJwtDto jwtConfiguracao)
        {
            _jwtConfiguracao = jwtConfiguracao;
        }

        public string GerarToken(UsuarioModel usuario)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Role, usuario.FuncaoId.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfiguracao.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtConfiguracao.Issuer,
                audience: _jwtConfiguracao.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtConfiguracao.ExpirationMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
