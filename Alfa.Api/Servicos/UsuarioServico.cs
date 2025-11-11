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
                if (usuarioExistente is null || !SenhaHash.VerificarSenhaHash(login.Email, usuarioExistente.SenhaHash, login.Senha))
                {
                    return null;
                }

                return new UsuarioAutenticadoDto
                {
                    Email = usuarioExistente.Email,
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

        public async Task<IEnumerable<UsuarioEmpresaDto>> ListarUsuariosEmpresaAsync(int empresaId)
        {
            if (empresaId == 0)
            {
                throw new BadHttpRequestException("Id da empresa inválido.");
            }

            try
            {
                return await _usuarioRepositorio.ListarUsuariosEmpresaAsync(empresaId);
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao listar usuários da empresa.", ex);
            }
        }

        public async Task CadastrarUsuarioAsync(UsuarioRegistroDto usuarioRegistro)
        {
            try
            {
                var usuarioExistente = await _usuarioRepositorio.BuscarUsuarioPorEmailAsync(usuarioRegistro.Email);
                if (usuarioExistente != null)
                {
                    throw new BadHttpRequestException("Usuário já existe com esse e-mail.");
                }

                int usuarioId = await _usuarioRepositorio.CadastrarUsuarioAsync(usuarioRegistro);

                if (usuarioId == 0)
                {
                    throw new Exception("Erro ao cadastrar usuário no banco.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao cadastrar novo usuário.", ex);
            }
        }

        public async Task<PermissoesUsuarioDto> ObterPermissoesPorUsuarioAsync(int usuarioId)
        {
            PermissoesUsuarioDto response = new();

            try
            {
                var permissoes = await _usuarioRepositorio.ObterPermissoesPorUsuarioIdAsync(usuarioId);

                response.UsuarioId = usuarioId;
                response.Permissoes = permissoes.ToList();

                return response;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao cadastrar novo usuário.", ex);
            }
        }
    }
}
