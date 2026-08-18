# ADR-002: EF Core + SQLite para persistência

- Status: Aceito
- Data: 2026-08-05

## Contexto

O app é mobile (MAUI/Android), local-first, sem backend. Precisa de um banco
embarcado com suporte a relacionamentos, índices e migrações.

## Decisão

- ORM: **Entity Framework Core 10** (provider `Microsoft.EntityFrameworkCore.Sqlite`).
- Banco: **SQLite** via `SQLitePCLRaw.bundle_e_sqlite3` **3.0.5**.
- Schema versionado por migrações EF (`src/GerenciadorDeFinancas.Persistence/Migrations`).
- Inicialização em runtime: `IDbInitializer` chama `Database.MigrateAsync()`
  (aplica migrações pendentes; não usa `EnsureCreated`).

## Consequências

- SQLitePCLRaw 3.x (não 2.1.12) porque 2.1.12 referencia
  `SQLitePCLRaw.provider.internal` inexistente no NuGet (NU1603) e 2.1.11
  possuía vulnerabilidade (NU1903). Validado pela suíte de testes que exercita
  SQLite real em memória.
- Migrações aplicadas automaticamente no start do app.
- Sem necessidade de migração manual de dados nesta fase.
