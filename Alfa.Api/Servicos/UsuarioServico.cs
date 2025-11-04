using Alfa.Api.Dtos;
using Alfa.Api.Modelos;
using Alfa.Api.Repositorios.Interfaces;
using Alfa.Api.Servicos.Interfaces;
using Alfa.Api.Utils;

namespace Alfa.Api.Servicos
{
    public class UsuarioServico : IUsuarioServico
    {
        private readonly IUsuarioRepositorio _usuarioRepositorio;
        private readonly ITokenServico _tokenServico;

        public UsuarioServico(IUsuarioRepositorio usuarioRepositorio, ITokenServico tokenServico)
        {
            _usuarioRepositorio = usuarioRepositorio;
            _tokenServico = tokenServico;
        }

        public async Task<UsuarioAutenticadoDto?> Login(UsuarioLoginDto login)
        {
            try
            {
                var usuarioExistente = await _usuarioRepositorio.BuscarUsuarioPorEmailAsync(login.Email);
                if (usuarioExistente == null || !SenhaHash.VerificarSenhaHash(login.Email, usuarioExistente.SenhaHash, login.Senha))
                {
                    return null;
                }

                return new UsuarioAutenticadoDto
                {
                    Email = usuarioExistente.Email,
                    FuncaoId = usuarioExistente.FuncaoId,
                    Token = _tokenServico.GerarToken(usuarioExistente)
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao fazer o login", ex);
            }

        }

        public async Task<UsuarioModel?> BuscarUsuarioPorEmailAsync(string email)
        {
            if (String.IsNullOrWhiteSpace(email)) {
                throw new BadHttpRequestException("E-mail inválido");
            }

            try
            {
                return await _usuarioRepositorio.BuscarUsuarioPorEmailAsync(email);
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao buscar usuário no banco.", ex);
            }
        }

        public async Task CadastrarUsuarioAsync(UsuarioRegistroDto usuarioRegistro)
        {
            try
            {
                var usuarioExistente = await _usuarioRepositorio.BuscarUsuarioPorEmailAsync(usuarioRegistro.Email);
                if (usuarioExistente != null)
                {
                    throw new BadHttpRequestException("Usuário já existe.");
                }

                await _usuarioRepositorio.CadastrarUsuarioAsync(usuarioRegistro);
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao cadastrar novo usuário.", ex);
            }
        }
    }
}
