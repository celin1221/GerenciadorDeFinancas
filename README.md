# GerenciadorDeFinancas

App Android (.NET MAUI) para gerenciar despesas compartilhadas em cartões de crédito. Captura compras automaticamente a partir de notificações do Android e controla divisões entre pessoas.

## Funcionalidades

- Captura automática de compras via notificações bancárias
- Classificação de compras com ações rápidas (notificação Android com botões)
- Divisão de despesas entre pessoas
- Controle de cartões de crédito por banco
- Dashboard com resumo de gastos

## Bancos suportados

Nubank, Mercado Pago, Inter, Banco do Brasil. Notificações de bancos não reconhecidos são ignoradas.

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Android SDK (local em `D:\AndroidSdk` ou ajustar o caminho)
- Visual Studio 2022+ com workload .NET MAUI (para build/debug)

## Build

```bash
dotnet build src/GerenciadorDeFinancas.Mobile/GerenciadorDeFinancas.csproj \
  -f net10.0-android -p:AndroidSdkDirectory="D:\AndroidSdk"
```

## Testes

```bash
dotnet test tests/GerenciadorDeFinancas.UnitTests/GerenciadorDeFinancas.UnitTests.csproj
```

Para rodar uma classe específica:

```bash
dotnet test tests/GerenciadorDeFinancas.UnitTests/GerenciadorDeFinancas.UnitTests.csproj \
  --filter "FullyQualifiedName~MoneyTests"
```

> **Importante:** Execute os testes apenas no projeto `UnitTests.csproj`, nunca na solução inteira — o app MAUI/Android exige o workload Android e falha em ambientes sem ele.

## Migrações

O dotnet-ef está instalado como ferramenta local (`.tools/`):

```bash
dotnet-ef migrations add <Nome> \
  --project src/GerenciadorDeFinancas.Persistence/GerenciadorDeFinancas.Persistence.csproj \
  --output-dir Migrations
```

## Arquitetura

Camadas com dependências apontando para dentro:

```
Domain          → (nenhuma dependência)
Application     → Domain
Infrastructure  → Application, Domain
Persistence     → Domain
Mobile          → todas (compõe DI)
```

Decisões de arquitetura documentadas em [`docs/adr/`](docs/adr/).

## Stack

- **UI:** .NET MAUI (Android)
- **ORM:** EF Core 10 + SQLite
- **Testes:** xUnit + SQLite em memória
- **Dinheiro:** `long` em centavos (value object `Money`)
- **Dedup:** SHA-256

## Licença

Este é um projeto privado.
