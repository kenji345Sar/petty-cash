using Microsoft.EntityFrameworkCore;
using PettyCash.Application.UseCases.Bags;
using PettyCash.Application.UseCases.CashBags;
using PettyCash.Application.UseCases.DenominationChecks;
using PettyCash.Application.UseCases.PrepBags;
using PettyCash.Application.UseCases.Transactions;
using PettyCash.Domain.Repositories;
using PettyCash.Infrastructure.Data;
using PettyCash.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<PettyCashDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IChangeBagRepository, ChangeBagRepository>();
builder.Services.AddScoped<ICashBagRepository, CashBagRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<DepositBagUseCase>();
builder.Services.AddScoped<MoveBagToRegisterUseCase>();
builder.Services.AddScoped<GetBagsUseCase>();
builder.Services.AddScoped<DepositCashBagUseCase>();
builder.Services.AddScoped<GetCashBagsUseCase>();
builder.Services.AddScoped<GetTransactionsUseCase>();
builder.Services.AddScoped<CreateTransactionUseCase>();
builder.Services.AddScoped<IDenominationCheckRepository, DenominationCheckRepository>();
builder.Services.AddScoped<CheckChangeBagUseCase>();
builder.Services.AddScoped<CheckCashBagUseCase>();
builder.Services.AddScoped<CheckPrepBagUseCase>();
builder.Services.AddScoped<GetDenominationChecksUseCase>();
builder.Services.AddScoped<UpdateDenominationCheckUseCase>();
builder.Services.AddScoped<IPrepBagRepository, PrepBagRepository>();
builder.Services.AddScoped<CreatePrepBagUseCase>();
builder.Services.AddScoped<GetPrepBagsUseCase>();
builder.Services.AddScoped<HandOverPrepBagUseCase>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PettyCashDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
