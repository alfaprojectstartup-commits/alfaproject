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
                if (usuarioExistente is null || !usuarioExistente.Ativo || !SenhaHash.VerificarSenhaHash(login.Email, usuarioExistente.SenhaHash, login.Senha))
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

        public async Task<UsuarioEmpresaDto?> BuscarUsuarioPorIdAsync(int usuarioId)
        {
            if (usuarioId == 0) {
                throw new BadHttpRequestException("UsuarioId inválido");
            }

            try
            {
                return await _usuarioRepositorio.BuscarUsuarioPorIdAsync(usuarioId);
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

        public async Task AtualizarDadosUsuarioAsync(UsuarioEmpresaDto usuario)
        {
            try
            {
                var usuarioExistente = await _usuarioRepositorio.BuscarUsuarioPorIdAsync(usuario.Id);
                if (usuarioExistente == null)
                {
                    throw new BadHttpRequestException($"Usuário não encontrado para o id: {usuario.Id}");
                }

                int usuarioAtualizado = await _usuarioRepositorio.AtualizarDadosUsuarioAsync(usuario);

                if (usuarioAtualizado == 0)
                {
                    throw new Exception("Erro ao atualizar dados do usuário.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao atualizar dados do usuário.", ex);
            }
        }

        public async Task<IEnumerable<PermissaoModel?>> ListarPermissoesSistemaAsync()
        {
            try
            {
                return await _usuarioRepositorio.ListarPermissoesSistemaAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao listar permissões do sistema.", ex);
            }
        }

        public async Task<UsuarioPermissoesUiDto> ObterUsuarioPermissoesUiAsync(int usuarioId)
        {
            UsuarioPermissoesUiDto response = new();

            try
            {
                var permissoes = await _usuarioRepositorio.ObterUsuarioPermissoesUiAsync(usuarioId);

                response.UsuarioId = usuarioId;
                response.Permissoes = permissoes.ToList();

                return response;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao obter permissões de ui do usuário.", ex);
            }
        }

        public async Task<IEnumerable<UsuarioPermissaoModel?>> ObterPermissoesUsuarioAsync(int usuarioId)
        {
            try
            {
                var permissoes = await _usuarioRepositorio.ObterPermissoesUsuarioAsync(usuarioId);
                return permissoes;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao obter permissões do usuário.", ex);
            }
        }
    }
}
