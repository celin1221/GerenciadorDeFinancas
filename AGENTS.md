# AGENTS.md

Guia para agentes de IA trabalharem neste repositório. O `README.md` cobre o
básico para humanos; aqui está o contexto que um agente provavelmente erraria
sem ajuda.

## Visão geral

App .NET MAUI (Android) "GerenciadorDeFinancas": gerenciar despesas
compartilhadas em cartões de crédito. Captura compras automaticamente a partir
de notificações do Android (NotificationListenerService), classifica por
notificação (ação rápida/divisão) e controla divisões entre pessoas.

Captura só ocorre para packages em `KnownBanks.KnownBankPackages`
(`Application/Banks/KnownBanks.cs`: Nubank — 2 packages incluindo
`com.nu.production`, Mercado Pago, Inter, BB) **e** quando o parser aceita a
notificação como compra (`PurchaseNotificationGate`). Package desconhecido é
ignorado antes de parsear. Classificação é manual: `ImportNotificationUseCase`
cria a compra `Pending` e chama `IClassificationPrompter`, que posta notificação
com botões rápidos (`PurchaseActionReceiver`); sem botões cadastrados, posta
aviso para cadastrar (action `OPEN_BUTTONS` no `MainActivity`).
`GenericNotificationParser` (`CanHandle=true`, prioridade 0) é fallback interno
do registry — não captura packages desconhecidos; bankId `generic` também é
opção de cartão ("Genérico / Outro banco").

## Stack e decisões importantes (ver `docs/adr/`)

- Parcelas = **compras independentes** (ADR-001).
- **EF Core 10 + SQLite** com migrações EF aplicadas no start (`MigrateAsync`,
  ADR-002). **Não usar `EnsureCreated`** em produção.
- `SQLitePCLRaw.bundle_e_sqlite3` **3.0.5** (2.1.11 tem NU1903; 2.1.12 tem
  NU1603 por dependência inexistente). ADR-002.
- Dinheiro = `long` em centavos, value object `Money` (ADR-003).
- Dedup de compras por SHA-256 (ADR-004).
- Parsers de notificação: registry por prioridade (ADR-005). Parser do banco
  retornando `null` = `ParseFailed`. Adicionar banco = novo parser + registro
  no DI + package em `KnownBanks`.
- Casos de uso diretos, **sem MediatR** (ADR-006).
- Arquitetura em camadas, dependências apontando para dentro (ADR-007).

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
→ `Domain`; `Infrastructure` → `Application` (+ `Domain` direto no csproj,
redundante); `Mobile` → todos (compõe DI).

## Comandos

O ambiente de build é **Windows** (projeto vive em `D:\Git\GerenciadorDeFinancas`,
montado no WSL como `/mnt/d/...`). No WSL o `dotnet` **não está no PATH**;
sempre use:

```bash
export DOTNET="/mnt/c/Program Files/dotnet/dotnet.exe"
"$DOTNET" test tests/GerenciadorDeFinancas.UnitTests/GerenciadorDeFinancas.UnitTests.csproj
```

- Build completo da solução (`.slnx` formato XML; Android SDK local em
  `D:\AndroidSdk`):
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
  falha no WSL. Suíte roda em SQLite em memória (**~146 testes**). Setup de teste
  usa `TestDb.CreateUnitOfWorkFactory()` (helper estático em `tests/`). Para rodar
  só uma classe:
  ```bash
  "$DOTNET" test tests/GerenciadorDeFinancas.UnitTests/GerenciadorDeFinancas.UnitTests.csproj --filter "FullyQualifiedName~MoneyTests"
  ```
- Testes de notificação via ADB (build DEBUG, app aberto no emulador/dispositivo
  ao menos uma vez — `MainApplication.Services` precisa existir):
  `TestNotificationReceiver` só existe em DEBUG e tem intent filter com action
  própria, então broadcast implícito dispensa o nome Java CRC64-hashed:
  ```cmd
  adb logcat -c
  adb shell am broadcast -a com.gerenciadordefinancas.TEST_NOTIFICATION --es package com.nubank.nubank --es title 'Compra aprovada' --es text 'Compra de R$ 50,00 em Padaria'
  adb logcat -s GDF_Test GDF_Capture GDF_Import GDF_Classify
  ```
  Se precisar do `-n`, descubra o nome hashado com:
  ```cmd
  adb shell dumpsys package com.companyname.gerenciadordefinancas | findstr TestNotif
  ```
  No Windows CMD, usar aspas simples (`'`) para extras com espaços — aspas duplas
  são engolidas pelo CMD.

## Convenções

- Pasta de build/artefatos e `.tools/` estão no `.gitignore`.
- `*.db3` não versionar (DB local de desenvolvimento).
- Entidades com props `private set` + NRT usam `= null!` (não `required` —
  evita CS9032 no EF).
- Novos `PurchaseShare` devem ser adicionados via
  `IPurchaseRepository.AddShare` (EF marca como `Modified` se não), ou usar
  `Purchase.SetShares` antes do primeiro save.
- `EnsureCreated` é usado **apenas** no `TestDb` (SQLite em memória); produção
  sempre via `MigrateAsync` (ADR-002).
- **Context do Android não está registrado no DI do MAUI**:
  `_services.GetService<Context>()` retorna null. Use
  `global::Android.App.Application.Context` como fallback. Em arquivos com
  `using GerenciadorDeFinancas.Application;`, `Application.Context` resolve para
  o namespace — qualifique com `global::`.
- Fluxo de pós-captura é centralizado: `ImportNotificationUseCase` →
  `IClassificationPrompter` → `NotificationClassificationPrompter` (posta
  notificação com até 3 botões de `NotificationButtons`, ou aviso "cadastre
  botões"). Não duplicar esse chamado nos callers.
- `POST_NOTIFICATIONS` (Android 13+) é solicitada uma vez no start do app;
  negada = `Notify()` descarta silenciosamente. Depurar notificações com
  `adb logcat -s GDF_Capture GDF_Classify`.
- Captura de notificações no aparelho exige o usuário conceder **"Acesso a
  notificações"** manualmente no sistema Android — a permissão
  `BIND_NOTIFICATION_LISTENER_SERVICE` não tem prompt em runtime.
- **Xiaomi/MIUI**: o MIUI mata serviços em background agressivamente. O
  `PurchaseNotificationListener` usa foreground service para sobreviver, mas
  o usuário também precisa habilitar: (1) Inicialização automática do app,
  (2) Bateria > Sem restrições, (3) Trancar o app no recent apps.
- Idioma das mensagens de erro de domínio: português.
- Não adicionar comentários em código salvo se necessário.
- Não há pipeline de CI/CD configurado no repositório.
- Cartão é opcional: quando o parser não encontra cartão registrado para o
  banco, `ImportNotificationUseCase` auto-cria um cartão genérico
  (`Last4Digits=null`) vinculado à primeira pessoa ativa. Se não houver
  pessoa ativa, retorna `CardNotMatched`.
- Tipos em `Platforms/Android/` só existem no TFM `net10.0-android`. Quando
  referenciados em código compartilhado (`MauiProgram.cs`, `App.xaml.cs`),
  usar `#if ANDROID` / `#else` com fallback (ex.: `NoOpClassificationPrompter`).
- Cores de pessoa são sempre salvas normalizadas `#RRGGBB` ou null (seletor
  visual no `PersonFormPage`); hex inválido quebra `HexToBrushConverter`.
