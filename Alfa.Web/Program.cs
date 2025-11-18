using Alfa.Web.Dtos;
using Alfa.Web.Filtros;
using Alfa.Web.Servicos;
using Alfa.Web.Servicos.Handlers;
using Alfa.Web.Servicos.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddHttpContextAccessor();

// ===== registrar o serviço de autenticação que criamos =====
builder.Services.AddScoped<IUsuarioServico, UsuarioServico>();
builder.Services.AddScoped<IAutenticacaoServico, AutenticacaoServico>();
builder.Services.AddTransient<JwtCookieHandler>();
builder.Services.AddScoped<ApiClient>();

builder.Services.AddScoped<IPermissaoUiServico, PermissaoUiServico>();

// ===== HttpClient para chamar a API =====
builder.Services.AddHttpClient("AlfaApiLogin", client =>
{
    var baseAddress = builder.Configuration["ApiBaseUrl"] ?? throw new InvalidOperationException("ApiBaseUrl não configurada.");
    client.BaseAddress = new Uri(baseAddress is not null ? baseAddress : "");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("AlfaApi", client =>
{
    var baseAddress = builder.Configuration["ApiBaseUrl"] ?? throw new InvalidOperationException("ApiBaseUrl não configurada.");
    client.BaseAddress = new Uri(baseAddress is not null ? baseAddress : "");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<JwtCookieHandler>();


// Configurações JWT
var configuracoesJwt = builder.Configuration.GetSection("ConfiguracoesJwt").Get<TokenJwtDto>()!;
builder.Services.AddSingleton(configuracoesJwt);

// ===== Authentication: JWT Bearer que lê o token do cookie "JwtToken" =====
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Autenticacao/Login";
    options.LogoutPath = "/Autenticacao/Logout";
    options.AccessDeniedPath = "/Autenticacao/AcessoNegado";
    options.ExpireTimeSpan = TimeSpan.FromHours(4);
})
.AddJwtBearer(options =>
{
    // IMPORTANT: configure validação de acordo com seu issuer/audience/chave
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;

    // Se você tem a chave pública ou validação, configure aqui:
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuracoesJwt.Issuer,
        ValidAudience = configuracoesJwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracoesJwt.Key)),
        NameClaimType = "nome"
    };

    // Puxar o token do cookie:
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            if (ctx.Request.Cookies.ContainsKey("JwtToken"))
            {
                ctx.Token = ctx.Request.Cookies["JwtToken"];
            }
            return Task.CompletedTask;
        }
    };
});

// Evita páginas serem cacheadas pelo navegador, evitando probleams ao clicar na seta "voltar" do navegador
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new FiltroCache());
});

builder.Services.AddAuthorization();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
}   
else
{
    builder.Services.AddRazorPages();
}
    
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.Name = ".Alfa.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAlfaWeb(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Autenticacao}/{action=Login}/{id?}");

app.Run();

