# ADR-003: Dinheiro como `long` em centavos

- Status: Aceito
- Data: 2026-08-05

## Contexto

Valores monetários em compras/divisões de cartão de crédito. `double`/`float`
produzem erros de arredondamento e `decimal` agrega custo e ruído em SQLite.

## Decisão

Criar o value object `Money` (`src/GerenciadorDeFinancas.Domain/ValueObjects/Money.cs`)
que encapsula `long Cents`. A persistência armazena apenas o inteiro em
centavos (`Purchase.AmountCents`, `PurchaseShare.AmountCents`).

## Consequências

- Operações aritméticas e comparações exatas com inteiros.
- `Money` oferece `SplitEvenlyIntoParts` para dividir de forma justa com
  arredondamento determinístico (diferença absorvida na primeira parcela).
- Regras de negócio validam soma das participações == valor total da compra
  (em `Purchase.SetShares`).
