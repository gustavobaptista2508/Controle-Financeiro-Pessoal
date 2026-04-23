using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Repositories;
using FinanceiroPessoal.WinForms.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Reutiliza o DbContext e repositórios já existentes
builder.Services.AddDbContext<MySqlDbContext>();
builder.Services.AddScoped<ILancamentoRepository, MySqlLancamentoRepository>();
builder.Services.AddScoped<ICadastroAuxiliarRepository, MySqlCadastroAuxiliarRepository>();
builder.Services.AddScoped<LancamentoService>();
builder.Services.AddScoped<CadastroAuxiliarService>();
builder.Services.AddScoped<DashboardService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();
app.Run();