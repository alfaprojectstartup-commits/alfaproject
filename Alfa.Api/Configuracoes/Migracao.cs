using Alfa.Api.Db;

namespace Alfa.Api.Configuracoes
{
    public static class Migracao
    {
        public static void Executar(string[] args, IConfiguration cfg, IWebHostEnvironment env, ILogger logger)
        {
            if (!args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
                return;

            // connection string: 1) param --cs, 2) env AZURE_SQL_CONNSTR, 3) appsettings
            string? csFromArg = args.SkipWhile(a => !a.Equals("--cs", StringComparison.OrdinalIgnoreCase))
                                    .Skip(1).FirstOrDefault();

            var cs = csFromArg
                     ?? Environment.GetEnvironmentVariable("AZURE_SQL_CONNSTR")
                     ?? cfg.GetConnectionString("Padrao")
                     ?? throw new InvalidOperationException("ConnectionString não encontrada.");

            var scriptsPath = Path.Combine(env.ContentRootPath, "Infra", "Scripts");
            Migracoes.Executar(cs, scriptsPath, logger);
        }
    }
}
