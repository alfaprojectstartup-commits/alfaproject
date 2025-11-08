using Polly;
using Polly.Extensions.Http;
using Alfa.Web.Servicos;
using Alfa.Web.Services;

namespace Alfa.Web.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAlfaWeb(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddHttpContextAccessor();
        services.AddDataProtection();
        services.AddTransient<EmpresaHeaderHandler>();
        services.AddSingleton<PreenchimentoExternoTokenService>();
        services.AddSingleton<IUrlProtector, UrlProtector>();

        services.AddSingleton<IProcessoPdfGenerator, ProcessoPdfGenerator>();

        services.AddHttpClient<ApiClient>(c =>
        {
            c.BaseAddress = new Uri(cfg["ApiBaseUrl"] ?? throw new InvalidOperationException("ApiBaseUrl não configurado"));
        })
        .AddHttpMessageHandler<EmpresaHeaderHandler>()
        .AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retry => TimeSpan.FromMilliseconds(200 * retry)));

        return services;
    }
}
