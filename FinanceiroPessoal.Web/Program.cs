using Stripe;
using ApexCharts;
using FinanceiroPessoal.Core.Repositories;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

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
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));
builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection("Billing"));
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrWhiteSpace(stripeSecretKey))
{
    StripeConfiguration.ApiKey = stripeSecretKey;
}
else
{
    builder.Logging.AddConsole();
    Console.WriteLine("Stripe não configurado.");
}
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
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
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


var mysqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(mysqlConnectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada.");
}

Console.WriteLine("MYSQL: connection string carregada.");
Console.WriteLine(mysqlConnectionString.Contains("Database=gadobd")
    ? "MYSQL: banco gadobd detectado."
    : "MYSQL: ATENÇÃO - banco diferente de gadobd.");

builder.Services.AddDbContext<FinanceiroPessoal.Core.Data.FinanceiroDbContext>(options =>
{
    options.UseMySql(mysqlConnectionString, new MySqlServerVersion(new Version(8, 0, 36)));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddScoped<WebAuthSessionService>();
builder.Services.AddScoped<UsuarioCadastroService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<LancamentoService>();
builder.Services.AddScoped<CadastroAuxiliarService>();
builder.Services.AddScoped<UsuarioPadraoService>();
builder.Services.AddScoped<ILancamentoRepository, MySqlLancamentoRepository>();
builder.Services.AddScoped<ICadastroAuxiliarRepository, MySqlCadastroAuxiliarRepository>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<IAssinaturaService, AssinaturaService>();
builder.Services.AddScoped<IStripeSubscriptionService, StripeSubscriptionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<TrialNotificationService>();
builder.Services.AddHostedService<TrialNotificationHostedService>();
builder.Services.AddHttpClient<IAssistenteFinanceiroIaService, AssistenteFinanceiroIaService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinanceiroPessoal.Core.Data.FinanceiroDbContext>();

    try
    {
        var totalPlanos = db.Planos.Count();
        var totalUsuarios = db.Usuarios.Count();

        Console.WriteLine($"TESTE MYSQL: Planos = {totalPlanos}");
        Console.WriteLine($"TESTE MYSQL: Usuarios = {totalUsuarios}");
    }
    catch (Exception ex)
    {
        Console.WriteLine("ERRO TESTE MYSQL:");
        Console.WriteLine(ex.ToString());
        throw;
    }
}

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

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
    var protegidos = new[] { "/dashboard", "/lancamentos", "/contas", "/categorias", "/pessoas", "/bancos", "/relatorios", "/ia" };
    var publico = path.StartsWith("/webhooks/stripe") || path.StartsWith("/planos") || path.StartsWith("/assinatura/") || path.StartsWith("/usuarios/cadastro") || path.StartsWith("/login") || path.StartsWith("/esqueci-senha") || path.StartsWith("/redefinir-senha") || path=="/" || path.StartsWith("/_framework") || path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/favicon");
    if (!publico && protegidos.Any(p => path.StartsWith(p)) && context.User.Identity?.IsAuthenticated == true)
    {
        var claim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claim, out var uid))
        {
            var svc = context.RequestServices.GetRequiredService<IAssinaturaService>();
            if (!await svc.UsuarioTemAcessoAsync(uid)) { context.Response.Redirect("/planos"); return; }
        }
    }
    await next();
});



app.MapPost("/auth/login", async (
    [FromBody] LoginRequest request,
    [FromServices] WebAuthSessionService session,
    HttpContext context) =>
{
    var result = await session.LoginWithPasswordAsync(request.Email, request.Senha, request.LembrarMe, context);
    return result.ok ? Results.Ok() : Results.BadRequest(new { message = result.erro ?? "Falha ao autenticar." });
});

app.MapPost("/auth/register", async (
    [FromBody] CadastroRequest request,
    [FromServices] UsuarioCadastroService cadastroService) =>
{
    var result = await cadastroService.CadastrarAsync(new FinanceiroPessoal.Web.Models.CadastroUsuarioModel
    {
        Nome = request.Nome,
        Email = request.Email,
        Senha = request.Senha,
        ConfirmarSenha = request.ConfirmarSenha
    });

    return result.Success ? Results.Ok() : Results.BadRequest(new { message = result.Message });
});

app.MapPost("/auth/logout", async ([FromServices] WebAuthSessionService session, HttpContext context) =>
{
    await session.LogoutAsync(context);
    return Results.Ok();
});

app.MapGet("/auth/logout", async ([FromServices] WebAuthSessionService session, HttpContext context) =>
{
    await session.LogoutAsync(context);
    return Results.Redirect("/login");
});

app.MapGet("/debug/mysql", async ([FromServices] FinanceiroPessoal.Core.Data.FinanceiroDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return canConnect ? Results.Ok("MySQL OK") : Results.Problem("Falha ao conectar MySQL");
    }
    catch
    {
        return Results.Problem("Não foi possível conectar ao banco de dados. Verifique a configuração do MySQL.");
    }
});

if (googleAuthConfigured)
{
    app.MapGet("/auth/google", async (HttpContext context) =>
    {
        var redirectUri = "/auth/google/callback";
        var properties = new AuthenticationProperties { RedirectUri = redirectUri };
        await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties);
    });

    app.MapGet("/auth/google/callback", async (HttpContext context, [FromServices] WebAuthSessionService session) =>
    {
        var principal = context.User;
        var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var nome = principal.Identity?.Name ?? email;

        if (string.IsNullOrWhiteSpace(email))
        {
            context.Response.Redirect("/login?erro=google_email_invalido");
            return;
        }

        try
        {
            await session.LoginWithGoogleAsync(email, nome ?? "Usuário Google", context);
            context.Response.Redirect("/dashboard");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG GOOGLE CALLBACK ERRO: {ex.Message}");
            context.Response.Redirect("/login?erro=mysql_indisponivel");
        }
    });
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();




app.MapPost("/api/ia/perguntar", async (HttpContext ctx, [FromBody] IaPerguntaRequest req, [FromServices] IAssistenteFinanceiroIaService ia) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var usuarioId = PluggyUserResolver.GetUsuarioId(ctx.User);
    var resposta = await ia.PerguntarAsync(usuarioId, req.Pergunta);
    return Results.Ok(new { resposta });
}).RequireAuthorization();

app.MapPost("/api/ia/resumo-mensal", async (HttpContext ctx, [FromBody] IaResumoRequest req, [FromServices] IAssistenteFinanceiroIaService ia) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var usuarioId = PluggyUserResolver.GetUsuarioId(ctx.User);
    var resposta = await ia.GerarResumoMensalAsync(usuarioId, req.Mes, req.Ano);
    return Results.Ok(new { resposta });
}).RequireAuthorization();

app.MapPost("/api/ia/analisar-categorias", async (HttpContext ctx, [FromBody] IaResumoRequest req, [FromServices] IAssistenteFinanceiroIaService ia) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var usuarioId = PluggyUserResolver.GetUsuarioId(ctx.User);
    var resposta = await ia.AnalisarCategoriasAsync(usuarioId, req.Mes, req.Ano);
    return Results.Ok(new { resposta });
}).RequireAuthorization();

app.MapPost("/api/pluggy/connect-token", async (HttpContext ctx, [FromServices] IPluggyService pluggy) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var usuarioId = PluggyUserResolver.GetUsuarioId(ctx.User);
    var token = await pluggy.CreateConnectTokenAsync(usuarioId);
    return Results.Ok(new ConnectTokenResponse(token));
}).RequireAuthorization();

app.MapPost("/api/pluggy/items", (HttpContext ctx, [FromBody] SaveItemRequest req, [FromServices] PluggyStore store) =>
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

app.MapGet("/api/pluggy/items/{itemId}/accounts", async (
    HttpContext ctx,
    [FromRoute] string itemId,
    [FromServices] IPluggyService pluggy,
    [FromServices] PluggyStore store) =>
{
    var usuarioId = PluggyUserResolver.GetUsuarioId(ctx.User);
    if (!store.Conexoes.Any(x => x.UsuarioId == usuarioId && x.ItemId == itemId)) return Results.Forbid();
    return Results.Ok(await pluggy.SyncAccountsAsync(usuarioId, itemId));
}).RequireAuthorization();

app.MapPost("/api/pluggy/accounts/{accountId}/transactions/sync", async (
    HttpContext ctx,
    [FromRoute] string accountId,
    [FromBody] SyncTransactionsRequest req,
    [FromServices] IPluggyService pluggy,
    [FromServices] PluggyStore store) =>
{
    var usuarioId = PluggyUserResolver.GetUsuarioId(ctx.User);
    if (!store.Contas.Any(x => x.UsuarioId == usuarioId && x.ExternalAccountId == accountId)) return Results.Forbid();
    return Results.Ok(await pluggy.SyncTransactionsAsync(usuarioId, accountId, req.DataInicial, req.DataFinal));
}).RequireAuthorization();

app.MapPost("/api/pluggy/transactions/{transacaoId:int}/transformar-lancamento", async (
    HttpContext ctx,
    [FromRoute] int transacaoId,
    [FromBody] ConvertTransactionRequest req,
    [FromServices] PluggyStore store,
    [FromServices] LancamentoService lancamentoService) =>
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


app.MapPost("/api/assinaturas/criar-checkout", async (HttpContext ctx, [FromBody] CriarCheckoutRequest request, [FromServices] IStripeSubscriptionService stripe) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var claim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(claim, out var usuarioId)) return Results.Unauthorized();
    try { var url = await stripe.CriarCheckoutSessionAsync(usuarioId, request.PlanoId); return Results.Ok(new { url }); }
    catch (Exception ex) { return Results.BadRequest(new { message = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/assinaturas/portal", async (HttpContext ctx,[FromServices] FinanceiroPessoal.Core.Data.FinanceiroDbContext db,[FromServices] IConfiguration cfg) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var claim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(claim, out var usuarioId)) return Results.Unauthorized();
    var user = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x=>x.Id==usuarioId);
    if (string.IsNullOrWhiteSpace(user?.StripeCustomerId)) return Results.BadRequest(new { message = "Usuário sem customer Stripe." });
    var session = await new Stripe.BillingPortal.SessionService().CreateAsync(new Stripe.BillingPortal.SessionCreateOptions{ Customer=user.StripeCustomerId, ReturnUrl=cfg["Stripe:PortalReturnUrl"] ?? cfg["Stripe:CancelUrl"] ?? "https://localhost:7073/planos"});
    return Results.Ok(new { url = session.Url });
}).RequireAuthorization();

app.MapPost("/webhooks/stripe", async (HttpRequest req, [FromServices] IStripeSubscriptionService stripe) =>
{
    using var reader = new StreamReader(req.Body);
    var json = await reader.ReadToEndAsync();
    var sig = req.Headers["Stripe-Signature"].ToString();
    try { await stripe.ProcessarWebhookAsync(json, sig); return Results.Ok(); }
    catch (StripeException) { return Results.BadRequest(); }
});

app.Run();

public sealed record LoginRequest(string Email, string Senha, bool LembrarMe);

public sealed record CadastroRequest(string Nome, string Email, string Senha, string ConfirmarSenha);

public sealed record CriarCheckoutRequest(int PlanoId);

record IaPerguntaRequest(string Pergunta);
record IaResumoRequest(int Mes, int Ano);
