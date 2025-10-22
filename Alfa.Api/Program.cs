using Alfa.Api.Dados;
using Alfa.Api.Repositorios;
using Alfa.Api.Db;
using Alfa.Api.Aplicacao;
using Alfa.Api.Repositorios.Interfaces;
using Alfa.Api.Aplicacao.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// serviços normais
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IConexaoSql, ConexaoSql>();
builder.Services.AddScoped<IProcessoRepositorio, ProcessoRepositorio>();
builder.Services.AddScoped<IProcessoApp, ProcessoApp>();

builder.Services.AddScoped<IFaseRepositorio, FaseRepositorio>();
builder.Services.AddScoped<IPaginaRepositorio, PaginaRepositorio>();
builder.Services.AddScoped<ICampoRepositorio, CampoRepositorio>();
builder.Services.AddScoped<IRespostaRepositorio, RespostaRepositorio>();


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

    var scriptsPath = Path.Combine(AppContext.BaseDirectory, "Infra", "Scripts");
    if (!Directory.Exists(scriptsPath))
        scriptsPath = Path.Combine(builder.Environment.ContentRootPath, "Infra", "Scripts");

    var code = Migracoes.Executar(cs, scriptsPath, logger);
    return;
}

var app = builder.Build();

app.Use(async (ctx, next) =>
{
    var h = ctx.Request.Headers["X-Empresa-Id"].FirstOrDefault();
    if (int.TryParse(h, out var empId) && empId > 0)
        ctx.Items["EmpresaId"] = empId;
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
