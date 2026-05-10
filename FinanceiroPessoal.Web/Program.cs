using ApexCharts;
using FinanceiroPessoal.Core.Services;
using FinanceiroPessoal.Web;
using FinanceiroPessoal.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Localization;
using MudBlazor.Services;
using FinanceiroPessoal.Web.Models;
using System.Security.Claims;
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
builder.Services.AddMemoryCache();
builder.Services.Configure<PluggyOptions>(builder.Configuration.GetSection("Pluggy"));
builder.Services.AddSingleton<PluggyStore>();
builder.Services.AddHttpClient<IPluggyAuthService, PluggyAuthService>();
builder.Services.AddHttpClient<IPluggyService, PluggyService>();


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



app.MapPost("/api/pluggy/connect-token", async (HttpContext ctx, IPluggyService pluggy) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var usuarioId = PluggyUserResolver.GetUsuarioId(ctx.User);
    var token = await pluggy.CreateConnectTokenAsync(usuarioId);
    return Results.Ok(new ConnectTokenResponse(token));
}).RequireAuthorization();

app.MapPost("/api/pluggy/items", (HttpContext ctx, SaveItemRequest req, PluggyStore store) =>
{
    var usuarioId = PluggyUserResolver.GetUsuarioId(ctx.User);
    var ex = store.Conexoes.FirstOrDefault(x => x.UsuarioId == usuarioId && x.ItemId == req.ItemId);
    if (ex is null)
    {
        ex = new ConexaoBancaria { Id = store.Conexoes.Count + 1, UsuarioId = usuarioId, ItemId = req.ItemId, NomeInstituicao = req.InstitutionName ?? req.ConnectorName, Status = req.Status ?? "UPDATED" };
        store.Conexoes.Add(ex);
    }
    else { ex.Status = req.Status ?? ex.Status; ex.NomeInstituicao = req.InstitutionName ?? ex.NomeInstituicao; ex.DataAtualizacao = DateTime.UtcNow; }
    return Results.Ok(ex);
}).RequireAuthorization();

app.MapGet("/api/pluggy/items/{itemId}/accounts", async (HttpContext ctx, string itemId, IPluggyService pluggy, PluggyStore store) =>
{
    var usuarioId = PluggyUserResolver.GetUsuarioId(ctx.User);
    if (!store.Conexoes.Any(x => x.UsuarioId == usuarioId && x.ItemId == itemId)) return Results.Forbid();
    return Results.Ok(await pluggy.SyncAccountsAsync(usuarioId, itemId));
}).RequireAuthorization();

app.MapPost("/api/pluggy/accounts/{accountId}/transactions/sync", async (HttpContext ctx, string accountId, SyncTransactionsRequest req, IPluggyService pluggy, PluggyStore store) =>
{
    var usuarioId = PluggyUserResolver.GetUsuarioId(ctx.User);
    if (!store.Contas.Any(x => x.UsuarioId == usuarioId && x.ExternalAccountId == accountId)) return Results.Forbid();
    return Results.Ok(await pluggy.SyncTransactionsAsync(usuarioId, accountId, req.DataInicial, req.DataFinal));
}).RequireAuthorization();

app.MapPost("/api/pluggy/transactions/{transacaoId:int}/transformar-lancamento", async (HttpContext ctx, int transacaoId, ConvertTransactionRequest req, PluggyStore store, LancamentoService lancamentoService) =>
{
    var usuarioId = PluggyUserResolver.GetUsuarioId(ctx.User);
    var tx = store.Transacoes.FirstOrDefault(x => x.Id == transacaoId && x.UsuarioId == usuarioId);
    if (tx is null) return Results.NotFound();
    if (tx.LancamentoId.HasValue) return Results.BadRequest("Transação já conciliada.");
    var similar = (await lancamentoService.ObterTodos()).FirstOrDefault(x => x.UsuarioId == usuarioId && x.DataVencimento?.Date == tx.DataTransacao.Date && x.Valor == Math.Abs(tx.Valor) && x.Descricao.Contains(tx.Descricao, StringComparison.OrdinalIgnoreCase));
    if (similar is not null && !req.IgnorarDuplicidade && !req.LancamentoIdVinculo.HasValue) return Results.Conflict(new { mensagem = "Possível duplicidade", lancamentoId = similar.Id });
    if (req.LancamentoIdVinculo.HasValue){ tx.LancamentoId=req.LancamentoIdVinculo; tx.Conciliado=true; return Results.Ok(tx);}    
    var novo = new FinanceiroPessoal.Core.Models.Lancamento { Descricao = tx.Descricao, Valor = Math.Abs(tx.Valor), Tipo = tx.Valor > 0 ? FinanceiroPessoal.Core.Models.TipoLancamento.Entrada : FinanceiroPessoal.Core.Models.TipoLancamento.Saida, Status = "Pago", DataPagamento = tx.DataTransacao, DataVencimento = tx.DataTransacao, Competencia = $"{tx.DataTransacao:MM/yyyy}", ContaId = req.ContaId, CategoriaId = req.CategoriaId, UsuarioId = usuarioId };
    await lancamentoService.Adicionar(novo); tx.LancamentoId = novo.Id; tx.Conciliado = true; return Results.Ok(tx);
}).RequireAuthorization();

app.Run();

internal sealed record LoginRequest(string Email, string Senha, bool LembrarMe);
