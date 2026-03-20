using Microsoft.EntityFrameworkCore;
using PettyCash.Application.UseCases.Bags;
using PettyCash.Application.UseCases.CashBags;
using PettyCash.Application.UseCases.DenominationChecks;
using PettyCash.Application.UseCases.PrepBags;
using PettyCash.Application.UseCases.Safes;
using PettyCash.Application.UseCases.Transactions;
using PettyCash.Domain.Repositories;
using PettyCash.Domain.Services;
using PettyCash.Infrastructure.Data;
using PettyCash.Infrastructure.Repositories;
using PettyCash.Infrastructure.Services;

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
builder.Services.AddScoped<IVendorTransactionRepository, VendorTransactionRepository>();
builder.Services.AddScoped<IPettyCashTransactionRepository, PettyCashTransactionRepository>();
builder.Services.AddScoped<ISequenceNumberService, SequenceNumberService>();
builder.Services.AddScoped<DepositBagUseCase>();
builder.Services.AddScoped<MoveBagToRegisterUseCase>();
builder.Services.AddScoped<GetBagsUseCase>();
builder.Services.AddScoped<DepositCashBagUseCase>();
builder.Services.AddScoped<GetCashBagsUseCase>();
builder.Services.AddScoped<GetVendorTransactionsUseCase>();
builder.Services.AddScoped<GetPettyCashTransactionsUseCase>();
builder.Services.AddScoped<CreatePettyCashTransactionUseCase>();
builder.Services.AddScoped<IDenominationCheckRepository, DenominationCheckRepository>();
builder.Services.AddScoped<CheckChangeBagUseCase>();
builder.Services.AddScoped<CheckCashBagUseCase>();
builder.Services.AddScoped<CheckPrepBagUseCase>();
builder.Services.AddScoped<CheckSafeUseCase>();
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
            ('名古屋', '名古屋店の金庫', NOW()),
            ('梅田', '梅田店の金庫', NOW()),
            ('銀座', '銀座店の金庫', NOW())");
    }

    // 「デフォルト金庫」「住吉」を「名古屋」にリネーム＋梅田・銀座が無ければ追加
    if (hasSafesTable > 0)
    {
        db.Database.ExecuteSqlRaw(@"
            UPDATE safes SET name = '名古屋', description = '名古屋店の金庫'
            WHERE name IN ('デフォルト金庫', '住吉')");
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

        // サンプル金庫を作成（既存データはID=1の名古屋に紐づけ）
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO safes (name, description, created_at) VALUES
            ('名古屋', '名古屋店の金庫', NOW()),
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

    // transactionsテーブルにsequence_numberカラムを追加
    db.Database.ExecuteSqlRaw(@"
        ALTER TABLE transactions ADD COLUMN IF NOT EXISTS sequence_number INTEGER NOT NULL DEFAULT 0
    ");
    // denomination_checksテーブルにsequence_numberカラムを追加
    db.Database.ExecuteSqlRaw(@"
        ALTER TABLE denomination_checks ADD COLUMN IF NOT EXISTS sequence_number INTEGER NOT NULL DEFAULT 0
    ");
    // 未採番データがある場合のみ連番を再計算
    var hasUnNumbered = db.Database.SqlQueryRaw<int>(
        "SELECT COUNT(*) AS \"Value\" FROM (SELECT 1 FROM transactions WHERE sequence_number = 0 UNION ALL SELECT 1 FROM denomination_checks WHERE sequence_number = 0) x"
    ).First();
    if (hasUnNumbered > 0)
    {
        db.Database.ExecuteSqlRaw(@"
            UPDATE transactions t SET sequence_number = sub.rn
            FROM (
                SELECT src, id, ROW_NUMBER() OVER (PARTITION BY safe_id ORDER BY created_at, src, id) AS rn
                FROM (
                    SELECT 'a' AS src, id, safe_id, created_at FROM transactions
                    UNION ALL
                    SELECT 'b' AS src, id, safe_id, created_at FROM denomination_checks
                ) combined
            ) sub
            WHERE sub.src = 'a' AND sub.id = t.id
        ");
        db.Database.ExecuteSqlRaw(@"
            UPDATE denomination_checks dc SET sequence_number = sub.rn
            FROM (
                SELECT src, id, ROW_NUMBER() OVER (PARTITION BY safe_id ORDER BY created_at, src, id) AS rn
                FROM (
                    SELECT 'a' AS src, id, safe_id, created_at FROM transactions
                    UNION ALL
                    SELECT 'b' AS src, id, safe_id, created_at FROM denomination_checks
                ) combined
            ) sub
            WHERE sub.src = 'b' AND sub.id = dc.id
        ");
    }

    // transactionsテーブルに金種カラムを追加
    db.Database.ExecuteSqlRaw(@"
        ALTER TABLE transactions ADD COLUMN IF NOT EXISTS denom_10000 INTEGER;
        ALTER TABLE transactions ADD COLUMN IF NOT EXISTS denom_5000 INTEGER;
        ALTER TABLE transactions ADD COLUMN IF NOT EXISTS denom_1000 INTEGER;
        ALTER TABLE transactions ADD COLUMN IF NOT EXISTS denom_500 INTEGER;
        ALTER TABLE transactions ADD COLUMN IF NOT EXISTS denom_100 INTEGER;
        ALTER TABLE transactions ADD COLUMN IF NOT EXISTS denom_50 INTEGER;
        ALTER TABLE transactions ADD COLUMN IF NOT EXISTS denom_10 INTEGER;
        ALTER TABLE transactions ADD COLUMN IF NOT EXISTS denom_5 INTEGER;
        ALTER TABLE transactions ADD COLUMN IF NOT EXISTS denom_1 INTEGER
    ");

    // transactionsテーブルを vendor_transactions / petty_cash_transactions に分割
    var hasOldTable = db.Database.SqlQueryRaw<int>(
        "SELECT COUNT(*) AS \"Value\" FROM information_schema.tables WHERE table_name = 'transactions'"
    ).First();

    if (hasOldTable > 0)
    {
        // vendor_transactions テーブル作成＆データ移行
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS vendor_transactions (
                id SERIAL PRIMARY KEY,
                sequence_number INTEGER NOT NULL DEFAULT 0,
                safe_id INTEGER NOT NULL REFERENCES safes(id),
                change_bag_id INTEGER,
                cash_bag_id INTEGER,
                prep_bag_id INTEGER,
                type INTEGER NOT NULL,
                amount INTEGER NOT NULL,
                description VARCHAR(200) NOT NULL DEFAULT '',
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                denom_10000 INTEGER,
                denom_5000 INTEGER,
                denom_1000 INTEGER,
                denom_500 INTEGER,
                denom_100 INTEGER,
                denom_50 INTEGER,
                denom_10 INTEGER,
                denom_5 INTEGER,
                denom_1 INTEGER
            )");

        // petty_cash_transactions テーブル作成
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS petty_cash_transactions (
                id SERIAL PRIMARY KEY,
                sequence_number INTEGER NOT NULL DEFAULT 0,
                safe_id INTEGER NOT NULL REFERENCES safes(id),
                type INTEGER NOT NULL,
                amount INTEGER NOT NULL,
                description VARCHAR(200) NOT NULL DEFAULT '',
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                denom_10000 INTEGER,
                denom_5000 INTEGER,
                denom_1000 INTEGER,
                denom_500 INTEGER,
                denom_100 INTEGER,
                denom_50 INTEGER,
                denom_10 INTEGER,
                denom_5 INTEGER,
                denom_1 INTEGER
            )");

        // 業者取引（バッグ紐づきあり）を移行
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO vendor_transactions (id, sequence_number, safe_id, change_bag_id, cash_bag_id, prep_bag_id, type, amount, description, created_at, denom_10000, denom_5000, denom_1000, denom_500, denom_100, denom_50, denom_10, denom_5, denom_1)
            SELECT id, sequence_number, safe_id, change_bag_id, cash_bag_id, prep_bag_id, type, amount, description, created_at, denom_10000, denom_5000, denom_1000, denom_500, denom_100, denom_50, denom_10, denom_5, denom_1
            FROM transactions
            WHERE change_bag_id IS NOT NULL OR cash_bag_id IS NOT NULL OR prep_bag_id IS NOT NULL
            ON CONFLICT (id) DO NOTHING");

        // 小口取引（バッグ紐づきなし）を移行
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO petty_cash_transactions (id, sequence_number, safe_id, type, amount, description, created_at, denom_10000, denom_5000, denom_1000, denom_500, denom_100, denom_50, denom_10, denom_5, denom_1)
            SELECT id, sequence_number, safe_id, type, amount, description, created_at, denom_10000, denom_5000, denom_1000, denom_500, denom_100, denom_50, denom_10, denom_5, denom_1
            FROM transactions
            WHERE change_bag_id IS NULL AND cash_bag_id IS NULL AND prep_bag_id IS NULL
            ON CONFLICT (id) DO NOTHING");

        // シーケンスを最大IDに合わせる
        db.Database.ExecuteSqlRaw(@"
            SELECT setval('vendor_transactions_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM vendor_transactions), 1));
            SELECT setval('petty_cash_transactions_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM petty_cash_transactions), 1))");

        // 旧テーブルを削除
        db.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS transactions CASCADE");
    }
}

app.Run();
