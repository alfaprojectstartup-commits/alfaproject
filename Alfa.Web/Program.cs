using Alfa.Web.Configuration;
using Alfa.Web.Servicos;
using Alfa.Web.Servicos.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


// ===== configuração da URL base da sua API (opcional) =====
builder.Configuration["ApiBaseUrl"] = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5001"; // ajuste

// ===== HttpClient para chamar a API =====
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ===== registrar o serviço de autenticação que criamos =====
builder.Services.AddScoped<IUsuarioServico, UsuarioServico>();

// ===== Authentication: JWT Bearer que lê o token do cookie "JwtToken" =====
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Usuario}/{action=Login}/{id?}");

app.Run();

