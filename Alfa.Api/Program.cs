using Alfa.Api.Configuracoes;

var builder = WebApplication.CreateBuilder(args);

// Servi�os
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.RegistrarDependencias();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Alfa API", Version = "v1" });
});

// Logging e CORS
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddCors(opt => opt.AddPolicy("DefaultCors", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Migra��es
if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
    var logger = loggerFactory.CreateLogger("Migracoes");
    Migracao.Executar(args, builder.Configuration, builder.Environment, logger);
    return;
}

var app = builder.Build();

// Middleware empresa
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Headers.TryGetValue("X-Empresa-Id", out var header) &&
        int.TryParse(header.FirstOrDefault(), out var empresaId) && empresaId > 0)
        ctx.Items["EmpresaId"] = empresaId;

    await next();
});

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Alfa API v1"));
}

app.UseHttpsRedirection();
app.UseCors("DefaultCors");
app.MapControllers();
app.Run();
