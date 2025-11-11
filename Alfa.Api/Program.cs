using Alfa.Api.Configuracoes;
using Alfa.Api.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.RegistrarDependencias();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Alfa API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira apenas o token JWT, sem o prefixo 'Bearer'."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configurações JWT
var configuracoesJwt = builder.Configuration.GetSection("ConfiguracoesJwt").Get<TokenJwtDto>()!;
builder.Services.AddSingleton(configuracoesJwt);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuracoesJwt.Issuer,
            ValidAudience = configuracoesJwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracoesJwt.Key))
        };
    });

builder.Services.AddAuthorization();

// Logging e CORS
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddCors(opt => opt.AddPolicy("DefaultCors", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Migrações
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
