using Alfa.Web.Configuration;
using Alfa.Web.Servicos;
using Alfa.Web.Servicos.Handlers;
using Alfa.Web.Servicos.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


// ===== configuração da URL base da sua API (opcional) =====
builder.Configuration["ApiBaseUrl"] = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5001"; // ajuste

builder.Services.AddHttpContextAccessor();

// ===== registrar o serviço de autenticação que criamos =====
builder.Services.AddScoped<IUsuarioServico, UsuarioServico>();
builder.Services.AddTransient<JwtCookieHandler>();


// ===== HttpClient para chamar a API =====
builder.Services.AddHttpClient("AlfaApi", client =>
{
    var baseAddress = builder.Configuration["ApiBaseUrl"];
    client.BaseAddress = new Uri(baseAddress is not null ? baseAddress : "");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<JwtCookieHandler>();


// ===== Authentication: JWT Bearer que lê o token do cookie "JwtToken" =====
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Usuario/Login";
    options.LogoutPath = "/Usuario/Logout";
    options.AccessDeniedPath = "/Usuario/AcessoNegado";
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
        ValidateIssuer = false, // ajustar conforme sua API
        ValidateAudience = false,
        ValidateIssuerSigningKey = false,
        ValidateLifetime = true
        // Se você tem a chave simétrica, setar IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("chave..."))
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

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation(); // Adicionar depois validação de ambiente para AddRazorRuntimeCompilation(), só usar em ambiente de dev

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(8);
    o.Cookie.Name = ".Alfa.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

builder.Services.AddAlfaWeb(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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
    pattern: "{controller=Usuario}/{action=Login}/{id?}");

app.Run();

