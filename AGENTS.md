# AGENTS.md

Guia para agentes de IA trabalharem neste repositório.

## Visão geral

App .NET MAUI (Android) "GerenciadorDeFinancas": gerenciar despesas
compartilhadas em cartões de crédito. Captura compras automaticamente a partir
de notificações do Android (NotificationListenerService), classifica por
notificação (ação rápida/divisão) e controla divisões entre pessoas. Tela
inicial: `DashboardPage` (resumo com totais por pessoa/cartão e status das
divisões), carregada pelo `AppShell` via DI (páginas resolvidas no container).

**Fase atual:** parsers dos 4 bancos (Nubank, Mercado Pago, Inter, BB)
implementados sobre keywords + helpers compartilhados (`BrCurrencyParser`
parcel-aware, `CardNumberParser`, `MerchantExtractor`), com corpus de teste de
formatos típicos — **ainda não validado com capturas reais** (refinar keywords).
`GenericNotificationParser` segue como último recurso para packages não
reconhecidos. Captura/feedback só ocorre para os 4 bancos e quando a
notificação tem forma de compra (`PurchaseNotificationGate`). A classificação é
manual (ver abaixo).

## Stack e decisões importantes (ver `docs/adr/`)

- Parcelas = **compras independentes** (ADR-001).
- **EF Core 10 + SQLite** com migrações EF aplicadas no start (`MigrateAsync`,
  ADR-002). **Não usar `EnsureCreated`** em produção.
- `SQLitePCLRaw.bundle_e_sqlite3` **3.0.5** (2.1.11 tem NU1903; 2.1.12 tem
  NU1603 por dependência inexistente). ADR-002.
- Dinheiro = `long` em centavos, value object `Money` (ADR-003).
- Dedup de compras por SHA-256 (ADR-004).
- Parsers de notificação: registry por prioridade + fallback genérico
  (ADR-005). Bancos v1: Nubank, Mercado Pago, Inter, Banco do Brasil
  (implementados em `Infrastructure/Notifications/Banks`). Sem fallback para
  packages de banco: parser do banco retornando `null` = `ParseFailed`.
- Classificação automática é stub: `IClassificationPrompter` é
  `NoOpClassificationPrompter` no app; compras nascem `Pending` e só viram
  `Classified` via `ClassifyPurchaseUseCase`/`SplitPurchaseUseCase` (ação na UI).
- Casos de uso diretos, **sem MediatR** (ADR-006).
- Arquitetura em camadas, direção de dependências apontando para dentro
  (ADR-007).

## Estrutura

```
src/
  GerenciadorDeFinancas.Domain/        # Entidades, Money, enums, abstrações
  GerenciadorDeFinancas.Application/   # DTOs, portas, casos de uso (UseCases/)
  GerenciadorDeFinancas.Infrastructure/ # Parsers de notificação, helpers
  GerenciadorDeFinancas.Persistence/   # EF Core, DbContext, repositórios, migrações
  GerenciadorDeFinancas.Mobile/        # MAUI (UI + composição de DI)
tests/GerenciadorDeFinancas.UnitTests/ # xUnit
docs/adr/                              # Registro de decisões (ADRs)
```

Regra de dependências: `Domain` não referencia nada; `Application`/`Persistence`
→ `Domain`; `Infrastructure` → `Application`; `Mobile` → todos (compõe DI).

## Comandos

O ambiente de build é **Windows** (projeto vive em `D:\ProjetosVisualStudio\...`,
montado no WSL como `/mnt/d/...`). No WSL o `dotnet` **não está no PATH**;
sempre use:

```bash
export DOTNET="/mnt/c/Program Files/dotnet/dotnet.exe"
"$DOTNET" test tests/GerenciadorDeFinancas.UnitTests/GerenciadorDeFinancas.UnitTests.csproj
```

- Build completo da solução (Android SDK local em `D:\AndroidSdk`):
  ```bash
  "$DOTNET" build GerenciadorDeFinancas.slnx -p:AndroidSdkDirectory="D:\AndroidSdk"
  ```
- Build só do app Android:
  ```bash
  "$DOTNET" build src/GerenciadorDeFinancas.Mobile/GerenciadorDeFinancas.csproj \
    -f net10.0-android -p:AndroidSdkDirectory="D:\AndroidSdk"
  ```
- Migrações (dotnet-ef instalado como ferramenta local em `.tools/`, fora do
  commit; manifest em `.config/dotnet-tools.json`):
  ```bash
  ".tools/dotnet-ef.exe" migrations add <Nome> \
    --project src/GerenciadorDeFinancas.Persistence/GerenciadorDeFinancas.Persistence.csproj \
    --output-dir Migrations
  ```
- Testes: **sempre** rodar no projeto `...UnitTests.csproj`, nunca na solução
  inteira — o `.slnx` inclui o app MAUI/Android, que exige o workload Android e
  falha no WSL. Suíte roda em SQLite em memória (**111 testes**). Para rodar só
  uma classe:
  ```bash
  "$DOTNET" test tests/GerenciadorDeFinancas.UnitTests/GerenciadorDeFinancas.UnitTests.csproj --filter "FullyQualifiedName~MoneyTests"
  ```

## Convenções

- Pasta de build/artefatos e `.tools/` estão no `.gitignore`.
- `*.db3` não versionar (DB local de desenvolvimento).
- Entidades com props `private set` + NRT usam `= null!` (não `required` —
  evita CS9032 no EF).
- Novos `PurchaseShare` devem ser adicionados via
  `IPurchaseRepository.AddShare` (EF marca como `Modified` se não), ou usar
  `Purchase.SetShares` antes do primeiro save.
- Banco de dados local de desenvolvimento não é versionado.
- `EnsureCreated` é usado **apenas** no `TestDb` (SQLite em memória); produção
  sempre via `MigrateAsync` (ADR-002).
- Captura de notificações no aparelho exige o usuário conceder **"Acesso a
  notificações"** manualmente no sistema Android — a permissão
  `BIND_NOTIFICATION_LISTENER_SERVICE` não tem prompt em runtime.
- Idioma das mensagens de erro de domínio: português.
- Não adicionar comentários em código salvo se necessário.
