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

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<WebAuthSessionService>();
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
