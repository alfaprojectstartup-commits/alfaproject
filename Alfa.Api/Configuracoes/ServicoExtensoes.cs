using Alfa.Api.Aplicacao;
using Alfa.Api.Aplicacao.Interfaces;
using Alfa.Api.Dados;
using Alfa.Api.Infra;
using Alfa.Api.Infra.Interfaces;
using Alfa.Api.Repositorios;
using Alfa.Api.Repositorios.Interfaces;

namespace Alfa.Api.Configuracoes
{
    public static class ServicoExtensoes
    {
        public static IServiceCollection RegistrarDependencias(this IServiceCollection services)
        {
            services.AddSingleton<IConexaoSql, ConexaoSql>();
            services.AddScoped<IProcessoRepositorio, ProcessoRepositorio>();
            services.AddScoped<IProcessoApp, ProcessoApp>();
            services.AddScoped<IFaseRepositorio, FaseRepositorio>();
            services.AddScoped<IPaginaRepositorio, PaginaRepositorio>();
            services.AddScoped<ICampoRepositorio, CampoRepositorio>();
            services.AddScoped<IRespostaRepositorio, RespostaRepositorio>();
            services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
            services.AddScoped<IExecucaoDb, ExecucaoDb>();

            return services;
        }


    }
}
