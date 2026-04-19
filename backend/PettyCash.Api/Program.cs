using Microsoft.EntityFrameworkCore;
using PettyCash.Application.UseCases.Bags;
using PettyCash.Application.UseCases.CashBags;
using PettyCash.Application.UseCases.DenominationChecks;
using PettyCash.Application.UseCases.PrepBags;
using PettyCash.Application.UseCases.Safes;
using PettyCash.Application.UseCases.Transactions;
using PettyCash.Application.UseCases.Dashboard;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Vendor.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.PettyCash.DenomCheck;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared.Services;
using PettyCash.Application.UseCases.Queries;
using PettyCash.Infrastructure.Data;
using PettyCash.Infrastructure.Queries;
using PettyCash.Infrastructure.Repositories;
using PettyCash.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<PettyCashDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PettyCashDbContext>());
builder.Services.AddScoped<ISafeRepository, SafeRepository>();
builder.Services.AddScoped<GetSafesUseCase>();
builder.Services.AddScoped<CreateSafeUseCase>();
builder.Services.AddScoped<IChangeBagRepository, ChangeBagRepository>();
builder.Services.AddScoped<ICashBagRepository, CashBagRepository>();
builder.Services.AddScoped<IVendorTransactionRepository, VendorTransactionRepository>();
builder.Services.AddScoped<IPettyCashTransactionRepository, PettyCashTransactionRepository>();
builder.Services.AddScoped<ISequenceNumberService, SequenceNumberService>();
builder.Services.AddScoped<IBalanceService, BalanceService>();
builder.Services.AddScoped<IProjectionService, ProjectionService>();
builder.Services.AddScoped<IEventStore, EventStore>();
builder.Services.AddScoped<IVendorLedgerQueryService, VendorLedgerQueryService>();
builder.Services.AddScoped<IPettyCashLedgerQueryService, PettyCashLedgerQueryService>();
builder.Services.AddScoped<DepositBagUseCase>();
builder.Services.AddScoped<MoveBagToRegisterUseCase>();
builder.Services.AddScoped<GetBagsUseCase>();
builder.Services.AddScoped<DepositCashBagUseCase>();
builder.Services.AddScoped<GetCashBagsUseCase>();
builder.Services.AddScoped<GetVendorTransactionsUseCase>();
builder.Services.AddScoped<GetPettyCashTransactionsUseCase>();
builder.Services.AddScoped<CreatePettyCashTransactionUseCase>();
builder.Services.AddScoped<IVendorDenominationCheckRepository, VendorDenominationCheckRepository>();
builder.Services.AddScoped<IPettyCashDenominationCheckRepository, PettyCashDenominationCheckRepository>();
builder.Services.AddScoped<CheckChangeBagUseCase>();
builder.Services.AddScoped<CheckCashBagUseCase>();
builder.Services.AddScoped<CheckPrepBagUseCase>();
builder.Services.AddScoped<CheckSafeUseCase>();
builder.Services.AddScoped<GetVendorDenominationChecksUseCase>();
builder.Services.AddScoped<GetPettyCashDenominationChecksUseCase>();
builder.Services.AddScoped<UpdateVendorDenominationCheckUseCase>();
builder.Services.AddScoped<UpdatePettyCashDenominationCheckUseCase>();
builder.Services.AddScoped<IPrepBagRepository, PrepBagRepository>();
builder.Services.AddScoped<CreatePrepBagUseCase>();
builder.Services.AddScoped<GetPrepBagsUseCase>();
builder.Services.AddScoped<HandOverPrepBagUseCase>();
builder.Services.AddScoped<CancelPrepBagUseCase>();
builder.Services.AddScoped<GetVendorDashboardUseCase>();
builder.Services.AddScoped<GetPettyCashDashboardUseCase>();

var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:5174" };
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<PettyCash.Api.Middleware.ExceptionHandlingMiddleware>();
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

            ALTER TABLE denomination_checks ADD COLUMN IF NOT EXISTS safe_id INTEGER;
            UPDATE denomination_checks SET safe_id = 1 WHERE safe_id IS NULL;
            ALTER TABLE denomination_checks ALTER COLUMN safe_id SET NOT NULL;
            ALTER TABLE denomination_checks ADD CONSTRAINT fk_denomination_checks_safe FOREIGN KEY (safe_id) REFERENCES safes(id)
        ");
    }

    // 旧transactionsテーブルが残っている場合のみマイグレーション実行
    // transactionsテーブルを vendor_transactions / petty_cash_transactions に分割
    var hasOldTable = db.Database.SqlQueryRaw<int>(
        "SELECT COUNT(*) AS \"Value\" FROM information_schema.tables WHERE table_name = 'transactions'"
    ).First();

    if (hasOldTable > 0)
    {
        // 旧テーブルにsafe_id・sequence_number・金種カラムを追加してからデータ移行
        db.Database.ExecuteSqlRaw(@"
            ALTER TABLE transactions ADD COLUMN IF NOT EXISTS safe_id INTEGER;
            UPDATE transactions SET safe_id = 1 WHERE safe_id IS NULL;
            ALTER TABLE transactions ALTER COLUMN safe_id SET NOT NULL;
            ALTER TABLE transactions ADD COLUMN IF NOT EXISTS sequence_number INTEGER NOT NULL DEFAULT 0;
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

    // Split denomination_checks into vendor/safe tables
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS vendor_denomination_checks (
            id SERIAL PRIMARY KEY,
            sequence_number INTEGER NOT NULL DEFAULT 0,
            safe_id INTEGER NOT NULL REFERENCES safes(id),
            change_bag_id INTEGER,
            cash_bag_id INTEGER,
            prep_bag_id INTEGER,
            checked_amount INTEGER NOT NULL,
            expected_amount INTEGER NOT NULL,
            difference INTEGER NOT NULL,
            created_at TIMESTAMP NOT NULL DEFAULT NOW(),
            count_10000 INTEGER NOT NULL DEFAULT 0,
            count_5000 INTEGER NOT NULL DEFAULT 0,
            count_1000 INTEGER NOT NULL DEFAULT 0,
            count_500 INTEGER NOT NULL DEFAULT 0,
            count_100 INTEGER NOT NULL DEFAULT 0,
            count_50 INTEGER NOT NULL DEFAULT 0,
            count_10 INTEGER NOT NULL DEFAULT 0,
            count_5 INTEGER NOT NULL DEFAULT 0,
            count_1 INTEGER NOT NULL DEFAULT 0
        )");
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS safe_denomination_checks (
            id SERIAL PRIMARY KEY,
            sequence_number INTEGER NOT NULL DEFAULT 0,
            safe_id INTEGER NOT NULL REFERENCES safes(id),
            checked_amount INTEGER NOT NULL,
            expected_amount INTEGER NOT NULL,
            difference INTEGER NOT NULL,
            created_at TIMESTAMP NOT NULL DEFAULT NOW(),
            count_10000 INTEGER NOT NULL DEFAULT 0,
            count_5000 INTEGER NOT NULL DEFAULT 0,
            count_1000 INTEGER NOT NULL DEFAULT 0,
            count_500 INTEGER NOT NULL DEFAULT 0,
            count_100 INTEGER NOT NULL DEFAULT 0,
            count_50 INTEGER NOT NULL DEFAULT 0,
            count_10 INTEGER NOT NULL DEFAULT 0,
            count_5 INTEGER NOT NULL DEFAULT 0,
            count_1 INTEGER NOT NULL DEFAULT 0
        )");
    // Migrate existing data
    var hasOldDenomTable = db.Database.SqlQueryRaw<int>(
        "SELECT COUNT(*) AS \"Value\" FROM information_schema.tables WHERE table_name = 'denomination_checks'"
    ).First();
    if (hasOldDenomTable > 0)
    {
        db.Database.ExecuteSqlRaw(@"
            ALTER TABLE denomination_checks ADD COLUMN IF NOT EXISTS sequence_number INTEGER NOT NULL DEFAULT 0
        ");
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO vendor_denomination_checks (id, sequence_number, safe_id, change_bag_id, cash_bag_id, prep_bag_id, checked_amount, expected_amount, difference, created_at, count_10000, count_5000, count_1000, count_500, count_100, count_50, count_10, count_5, count_1)
            SELECT id, sequence_number, safe_id, change_bag_id, cash_bag_id, prep_bag_id, checked_amount, expected_amount, difference, created_at, count_10000, count_5000, count_1000, count_500, count_100, count_50, count_10, count_5, count_1
            FROM denomination_checks
            WHERE change_bag_id IS NOT NULL OR cash_bag_id IS NOT NULL OR prep_bag_id IS NOT NULL
            ON CONFLICT (id) DO NOTHING");
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO safe_denomination_checks (id, sequence_number, safe_id, checked_amount, expected_amount, difference, created_at, count_10000, count_5000, count_1000, count_500, count_100, count_50, count_10, count_5, count_1)
            SELECT id, sequence_number, safe_id, checked_amount, expected_amount, difference, created_at, count_10000, count_5000, count_1000, count_500, count_100, count_50, count_10, count_5, count_1
            FROM denomination_checks
            WHERE change_bag_id IS NULL AND cash_bag_id IS NULL AND prep_bag_id IS NULL
            ON CONFLICT (id) DO NOTHING");
        // Set sequences
        db.Database.ExecuteSqlRaw(@"
            SELECT setval('vendor_denomination_checks_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM vendor_denomination_checks), 1));
            SELECT setval('safe_denomination_checks_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM safe_denomination_checks), 1))");
    }
    // ランニングバランス（balance列）の追加マイグレーション
    var hasVendorBalanceCol = db.Database.SqlQueryRaw<int>(
        "SELECT COUNT(*) AS \"Value\" FROM information_schema.columns WHERE table_name = 'vendor_transactions' AND column_name = 'balance'"
    ).First();

    if (hasVendorBalanceCol == 0)
    {
        // カラム追加
        db.Database.ExecuteSqlRaw(@"
            ALTER TABLE vendor_transactions ADD COLUMN balance INTEGER NOT NULL DEFAULT 0;
            ALTER TABLE petty_cash_transactions ADD COLUMN balance INTEGER NOT NULL DEFAULT 0");

        // 既存データの遡及計算（時系列順にランニングバランスを算出）
        db.Database.ExecuteSqlRaw(@"
            WITH running AS (
                SELECT id,
                       SUM(CASE WHEN type IN (0, 2) THEN amount ELSE -amount END)
                           OVER (PARTITION BY safe_id ORDER BY created_at, id) AS bal
                FROM vendor_transactions
            )
            UPDATE vendor_transactions SET balance = running.bal
            FROM running WHERE vendor_transactions.id = running.id");

        db.Database.ExecuteSqlRaw(@"
            WITH running AS (
                SELECT id,
                       SUM(CASE WHEN type IN (0, 2) THEN amount ELSE -amount END)
                           OVER (PARTITION BY safe_id ORDER BY created_at, id) AS bal
                FROM petty_cash_transactions
            )
            UPDATE petty_cash_transactions SET balance = running.bal
            FROM running WHERE petty_cash_transactions.id = running.id");
    }
    // Read Model テーブルの作成マイグレーション
    var hasSafeBalances = db.Database.SqlQueryRaw<int>(
        "SELECT COUNT(*) AS \"Value\" FROM information_schema.tables WHERE table_name = 'safe_balances'"
    ).First();

    if (hasSafeBalances == 0)
    {
        // safe_balances（金庫残高 Read Model）
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE safe_balances (
                safe_id INTEGER PRIMARY KEY REFERENCES safes(id),
                vendor_balance INTEGER NOT NULL DEFAULT 0,
                petty_cash_balance INTEGER NOT NULL DEFAULT 0,
                updated_at TIMESTAMP NOT NULL DEFAULT NOW()
            )");

        // vendor_ledger_view（業者出納帳 Read Model）
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE vendor_ledger_view (
                id SERIAL PRIMARY KEY,
                sequence_number INTEGER NOT NULL,
                safe_id INTEGER NOT NULL,
                change_bag_id INTEGER,
                cash_bag_id INTEGER,
                prep_bag_id INTEGER,
                type INTEGER NOT NULL,
                amount INTEGER NOT NULL,
                balance INTEGER NOT NULL,
                description VARCHAR(200) NOT NULL DEFAULT '',
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            )");

        // petty_cash_ledger_view（小口出納帳 Read Model）
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE petty_cash_ledger_view (
                id SERIAL PRIMARY KEY,
                sequence_number INTEGER NOT NULL,
                safe_id INTEGER NOT NULL,
                type INTEGER NOT NULL,
                amount INTEGER NOT NULL,
                balance INTEGER NOT NULL,
                description VARCHAR(200) NOT NULL DEFAULT '',
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            )");

        // 既存データからRead Modelを初期構築
        // safe_balances: 各金庫の最新残高をイベントストアから算出
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO safe_balances (safe_id, vendor_balance, petty_cash_balance, updated_at)
            SELECT s.id,
                COALESCE((SELECT balance FROM vendor_transactions WHERE safe_id = s.id ORDER BY created_at DESC, id DESC LIMIT 1), 0),
                COALESCE((SELECT balance FROM petty_cash_transactions WHERE safe_id = s.id ORDER BY created_at DESC, id DESC LIMIT 1), 0),
                NOW()
            FROM safes s");

        // vendor_ledger_view: イベントストアの全データをコピー
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO vendor_ledger_view (id, sequence_number, safe_id, change_bag_id, cash_bag_id, prep_bag_id, type, amount, balance, description, created_at)
            SELECT id, sequence_number, safe_id, change_bag_id, cash_bag_id, prep_bag_id, type, amount, balance, description, created_at
            FROM vendor_transactions");

        // petty_cash_ledger_view: イベントストアの全データをコピー
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO petty_cash_ledger_view (id, sequence_number, safe_id, type, amount, balance, description, created_at)
            SELECT id, sequence_number, safe_id, type, amount, balance, description, created_at
            FROM petty_cash_transactions");

        // シーケンスを合わせる
        db.Database.ExecuteSqlRaw(@"
            SELECT setval('vendor_ledger_view_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM vendor_ledger_view), 1));
            SELECT setval('petty_cash_ledger_view_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM petty_cash_ledger_view), 1))");
    }
    // domain_events（イベントストア）テーブルの作成マイグレーション
    var hasDomainEvents = db.Database.SqlQueryRaw<int>(
        "SELECT COUNT(*) AS \"Value\" FROM information_schema.tables WHERE table_name = 'domain_events'"
    ).First();

    if (hasDomainEvents == 0)
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE domain_events (
                id BIGSERIAL PRIMARY KEY,
                aggregate_type VARCHAR(100) NOT NULL,
                aggregate_id INTEGER NOT NULL,
                event_type VARCHAR(100) NOT NULL,
                payload JSONB NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            )");

        db.Database.ExecuteSqlRaw(@"
            CREATE INDEX idx_domain_events_aggregate ON domain_events (aggregate_type, aggregate_id)");

        // 既存トランザクションからイベントを遡及生成
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO domain_events (aggregate_type, aggregate_id, event_type, payload, created_at)
            SELECT
                'Safe', safe_id,
                CASE type WHEN 0 THEN 'VendorMoneyDeposited' WHEN 1 THEN 'VendorMoneyWithdrawn' ELSE 'VendorBalanceAdjusted' END,
                jsonb_build_object(
                    'safeId', safe_id, 'amount', amount, 'balance', balance,
                    'description', description, 'sequenceNumber', sequence_number,
                    'changeBagId', change_bag_id, 'cashBagId', cash_bag_id, 'prepBagId', prep_bag_id
                ),
                created_at
            FROM vendor_transactions
            ORDER BY created_at, id");

        db.Database.ExecuteSqlRaw(@"
            INSERT INTO domain_events (aggregate_type, aggregate_id, event_type, payload, created_at)
            SELECT
                'Safe', safe_id,
                CASE type WHEN 0 THEN 'PettyCashDeposited' WHEN 1 THEN 'PettyCashWithdrawn' ELSE 'PettyCashBalanceAdjusted' END,
                jsonb_build_object(
                    'safeId', safe_id, 'amount', amount, 'balance', balance,
                    'description', description, 'sequenceNumber', sequence_number
                ),
                created_at
            FROM petty_cash_transactions
            ORDER BY created_at, id");
    }
}

app.Run();
