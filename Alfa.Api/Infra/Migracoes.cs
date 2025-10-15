using DbUp;
using Microsoft.Extensions.Logging;

namespace Alfa.Api.Db;

public static class Migracoes
{
    public static int Executar(string connectionString, string scriptsPath, ILogger logger)
    {
        var upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsFromFileSystem(scriptsPath)
            .LogToAutodetectedLog()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            logger.LogError(result.Error, "Falha ao aplicar migrations");
            return -1;
        }

        logger.LogInformation("Migrations aplicadas com sucesso.");
        return 0;
    }
}
