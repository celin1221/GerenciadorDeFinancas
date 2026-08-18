# ADR-006: Casos de uso diretos (sem MediatR)

- Status: Aceito
- Data: 2026-08-05

## Contexto

O `Application` precisa expor operações de negócio de forma testável e com
dependências controladas (notificações Android, prompt de classificação).

## Decisão

Casos de uso como **classes simples com injeção de dependência** em
`src/GerenciadorDeFinancas.Application/UseCases/`, **sem MediatR**:

- `ImportNotificationUseCase` — parser → dedup → card/merchant/statement → prompt.
- `ClassifyPurchaseUseCase` — atribui categoria e encerra classificação.
- `SplitPurchaseUseCase` — divisão igualitária ou custom entre pessoas.

Dependências externas (parser, prompt) são abstraídas por **portas**
(`INotificationParserRegistry`, `IClassificationPrompter`) injetadas no
constructor.

## Consequências

- Menos indireção que MediatR; chamadas explícitas e fáceis de seguir.
- Testável com dublês das portas e `IUnitOfWorkFactory` (SQLite em memória).
- Trocar por MediatR no futuro é trivial se volume de cross-cutting crescer.
