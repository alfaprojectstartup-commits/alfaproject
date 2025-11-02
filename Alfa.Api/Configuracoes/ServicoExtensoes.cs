using Alfa.Api.Aplicacao;
using Alfa.Api.Aplicacao.Interfaces;
using Alfa.Api.Dados;
using Alfa.Api.Infra;
using Alfa.Api.Infra.Interfaces;
using Alfa.Api.Repositorios;
using Alfa.Api.Repositorios.Interfaces;
using Alfa.Api.Servicos;
using Alfa.Api.Servicos.Interfaces;

namespace Alfa.Api.Configuracoes
{
    public static class ServicoExtensoes
    {
        public static IServiceCollection RegistrarDependencias(this IServiceCollection services)
        {
            // Repositorios
            services.AddScoped<IProcessoRepositorio, ProcessoRepositorio>();
            services.AddScoped<IProcessoPadraoRepositorio, ProcessoPadraoRepositorio>();
            services.AddScoped<IFaseRepositorio, FaseRepositorio>();
            services.AddScoped<IPaginaRepositorio, PaginaRepositorio>();
            services.AddScoped<ICampoRepositorio, CampoRepositorio>();
            services.AddScoped<IRespostaRepositorio, RespostaRepositorio>();
            services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();

            // Infraestrutura
            services.AddSingleton<IConexaoSql, ConexaoSql>();
            services.AddScoped<IExecucaoDb, ExecucaoDb>();

            // Servicos
            services.AddScoped<IProcessoApp, ProcessoApp>();
            services.AddScoped<ITokenServico, TokenServico>();
            services.AddScoped<IUsuarioServico, UsuarioServico>();

            return services;
        }

    }
}
