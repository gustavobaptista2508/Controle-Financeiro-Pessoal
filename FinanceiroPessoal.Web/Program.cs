using ApexCharts;
using FinanceiroPessoal.Core.Services;
using FinanceiroPessoal.Web;
using FinanceiroPessoal.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Localization;
using MudBlazor.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();
builder.Services.AddApexCharts();
builder.Services.AddMudServices();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleAuthConfigured = !string.IsNullOrWhiteSpace(googleClientId) &&
                           !string.IsNullOrWhiteSpace(googleClientSecret);

var authBuilder = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = googleAuthConfigured
            ? GoogleDefaults.AuthenticationScheme
            : CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    });

if (googleAuthConfigured)
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
    });
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<WebAuthSessionService>();
builder.Services.AddScoped<UsuarioCadastroService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

var culturaPtBr = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culturaPtBr;
CultureInfo.DefaultThreadCurrentUICulture = culturaPtBr;

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culturaPtBr),
    SupportedCultures = [culturaPtBr],
    SupportedUICultures = [culturaPtBr]
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();


app.MapPost("/auth/login", async (LoginRequest request, WebAuthSessionService session, HttpContext context) =>
{
    var result = await session.LoginWithPasswordAsync(request.Email, request.Senha, request.LembrarMe, context);
    return result.ok ? Results.Ok() : Results.BadRequest(new { message = result.erro ?? "Falha ao autenticar." });
});

if (googleAuthConfigured)
{
    app.MapGet("/auth/google", async (HttpContext context) =>
    {
        var redirectUri = "/auth/google/callback";
        var properties = new AuthenticationProperties { RedirectUri = redirectUri };
        await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties);
    });

    app.MapGet("/auth/google/callback", async (HttpContext context, WebAuthSessionService session) =>
    {
        var principal = context.User;
        var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var nome = principal.Identity?.Name ?? email;

        if (string.IsNullOrWhiteSpace(email))
        {
            context.Response.Redirect("/login?erro=google_email_invalido");
            return;
        }

        await session.LoginWithGoogleAsync(email, nome ?? "Usuário Google", context);
        context.Response.Redirect("/dashboard");
    });
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

internal sealed record LoginRequest(string Email, string Senha, bool LembrarMe);
