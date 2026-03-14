using Microsoft.EntityFrameworkCore;
using PettyCash.Application.UseCases.Bags;
using PettyCash.Application.UseCases.CashBags;
using PettyCash.Application.UseCases.DenominationChecks;
using PettyCash.Application.UseCases.PrepBags;
using PettyCash.Application.UseCases.Safes;
using PettyCash.Application.UseCases.Transactions;
using PettyCash.Domain.Repositories;
using PettyCash.Infrastructure.Data;
using PettyCash.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<PettyCashDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ISafeRepository, SafeRepository>();
builder.Services.AddScoped<GetSafesUseCase>();
builder.Services.AddScoped<CreateSafeUseCase>();
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

    // safesテーブルが存在しない場合（既存DBへのマイグレーション）
    var hasSafesTable = db.Database.SqlQueryRaw<int>(
        "SELECT COUNT(*) AS \"Value\" FROM information_schema.tables WHERE table_name = 'safes'"
    ).First();

    // 金庫が0件の場合はシード（新規DBの場合）
    if (hasSafesTable > 0 && !db.Safes.Any())
    {
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO safes (name, description, created_at) VALUES
            ('住吉', '住吉店の金庫', NOW()),
            ('梅田', '梅田店の金庫', NOW()),
            ('銀座', '銀座店の金庫', NOW())");
    }

    // 「デフォルト金庫」を「住吉」にリネーム＋梅田・銀座が無ければ追加
    if (hasSafesTable > 0)
    {
        db.Database.ExecuteSqlRaw(@"
            UPDATE safes SET name = '住吉', description = '住吉店の金庫'
            WHERE name = 'デフォルト金庫'");
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO safes (name, description, created_at)
            SELECT '梅田', '梅田店の金庫', NOW()
            WHERE NOT EXISTS (SELECT 1 FROM safes WHERE name = '梅田')");
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO safes (name, description, created_at)
            SELECT '銀座', '銀座店の金庫', NOW()
            WHERE NOT EXISTS (SELECT 1 FROM safes WHERE name = '銀座')");
    }

    if (hasSafesTable == 0)
    {
        // safesテーブルを作成
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE safes (
                id SERIAL PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                description VARCHAR(200) NOT NULL DEFAULT '',
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            )");

        // サンプル金庫を作成（既存データはID=1の住吉に紐づけ）
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO safes (name, description, created_at) VALUES
            ('住吉', '住吉店の金庫', NOW()),
            ('梅田', '梅田店の金庫', NOW()),
            ('銀座', '銀座店の金庫', NOW())");

        // 各テーブルにsafe_idカラムを追加し、既存データをデフォルト金庫に紐づけ
        db.Database.ExecuteSqlRaw(@"
            ALTER TABLE change_bags ADD COLUMN IF NOT EXISTS safe_id INTEGER;
            UPDATE change_bags SET safe_id = 1 WHERE safe_id IS NULL;
            ALTER TABLE change_bags ALTER COLUMN safe_id SET NOT NULL;
            ALTER TABLE change_bags ADD CONSTRAINT fk_change_bags_safe FOREIGN KEY (safe_id) REFERENCES safes(id);

            ALTER TABLE cash_bags ADD COLUMN IF NOT EXISTS safe_id INTEGER;
            UPDATE cash_bags SET safe_id = 1 WHERE safe_id IS NULL;
            ALTER TABLE cash_bags ALTER COLUMN safe_id SET NOT NULL;
            ALTER TABLE cash_bags ADD CONSTRAINT fk_cash_bags_safe FOREIGN KEY (safe_id) REFERENCES safes(id);

            ALTER TABLE prep_bags ADD COLUMN IF NOT EXISTS safe_id INTEGER;
            UPDATE prep_bags SET safe_id = 1 WHERE safe_id IS NULL;
            ALTER TABLE prep_bags ALTER COLUMN safe_id SET NOT NULL;
            ALTER TABLE prep_bags ADD CONSTRAINT fk_prep_bags_safe FOREIGN KEY (safe_id) REFERENCES safes(id);

            ALTER TABLE transactions ADD COLUMN IF NOT EXISTS safe_id INTEGER;
            UPDATE transactions SET safe_id = 1 WHERE safe_id IS NULL;
            ALTER TABLE transactions ALTER COLUMN safe_id SET NOT NULL;
            ALTER TABLE transactions ADD CONSTRAINT fk_transactions_safe FOREIGN KEY (safe_id) REFERENCES safes(id);

            ALTER TABLE denomination_checks ADD COLUMN IF NOT EXISTS safe_id INTEGER;
            UPDATE denomination_checks SET safe_id = 1 WHERE safe_id IS NULL;
            ALTER TABLE denomination_checks ALTER COLUMN safe_id SET NOT NULL;
            ALTER TABLE denomination_checks ADD CONSTRAINT fk_denomination_checks_safe FOREIGN KEY (safe_id) REFERENCES safes(id)
        ");
    }
}

app.Run();
