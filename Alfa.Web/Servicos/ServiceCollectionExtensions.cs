using Polly;
using Polly.Extensions.Http;
using Alfa.Web.Servicos;
using Alfa.Web.Servicos.Handlers; // <- para usar JwtCookieHandler/EmpresaHeaderHandler
using Alfa.Web.Servicos.Interfaces;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAlfaWeb(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddHttpContextAccessor();
        services.AddDataProtection();

        services.AddTransient<EmpresaHeaderHandler>();
        services.AddTransient<JwtCookieHandler>();
        services.AddSingleton<PreenchimentoExternoTokenService>();
        services.AddSingleton<IUrlProtector, UrlProtector>();
        services.AddSingleton<IProcessoPdfGenerator, ProcessoPdfGenerator>();

        services.AddHttpClient("AlfaApi", c =>
        {
            c.BaseAddress = new Uri(cfg["ApiBaseUrl"]
                ?? throw new InvalidOperationException("ApiBaseUrl não configurado"));
        })
        .AddHttpMessageHandler<JwtCookieHandler>()
        .AddHttpMessageHandler<EmpresaHeaderHandler>()
        .AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retry => TimeSpan.FromMilliseconds(200 * retry)));

        services.AddScoped<IProcessoServico, ProcessoServico>();
        services.AddScoped<IFaseServico, FaseServico>();

        return services;
    }
}
