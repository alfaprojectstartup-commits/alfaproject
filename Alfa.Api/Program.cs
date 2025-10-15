using Alfa.Api.Dados;
using Alfa.Api.Repositorios;
using Alfa.Api.Db;
using Alfa.Api.Aplicacao;

var builder = WebApplication.CreateBuilder(args);

// serviços normais
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IConexaoSql, ConexaoSql>();
builder.Services.AddScoped<IProcessoRepositorio, ProcessoRepositorio>();
builder.Services.AddScoped<IProcessoApp, ProcessoApp>();

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
    var logger = loggerFactory.CreateLogger("Migracoes");

    // connection string: 1) param --cs, 2) env AZURE_SQL_CONNSTR, 3) appsettings
    string? csFromArg = null;
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i].Equals("--cs", StringComparison.OrdinalIgnoreCase))
            csFromArg = args[i + 1];

    var cfg = builder.Configuration;
    var cs = csFromArg
             ?? Environment.GetEnvironmentVariable("AZURE_SQL_CONNSTR")
             ?? cfg.GetConnectionString("Padrao")
             ?? throw new InvalidOperationException("ConnectionString não encontrada.");

    var scriptsPath = Path.Combine(AppContext.BaseDirectory, "Db", "Scripts");
    if (!Directory.Exists(scriptsPath))
        scriptsPath = Path.Combine(builder.Environment.ContentRootPath, "Db", "Scripts");

    var code = Migracoes.Executar(cs, scriptsPath, logger);
    return;
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
