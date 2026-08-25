# ADR-008: Faturas automáticas e parcelamento por declaração

## Status

Aceito (2026-08)

## Contexto

Compras no crédito costumam ser parceladas, e bancos nem sempre informam a
parcela na notificação. Além disso, o app não tinha como registrar compras que
escaparam da captura nem compras anteriores ao app. A entidade `Statement`
existia desde o início, mas nunca era criada em produção — toda compra nascia
com `StatementId = null`.

## Decisão

1. **Atribuição automática de fatura.** Toda compra (capturada, manual ou
   parcela) é atribuída à fatura do seu mês de referência via
   `StatementResolver` (Application), que calcula o período com
   `StatementPeriodCalculator`: dia da compra ≤ `Card.ClosingDay` → fatura do
   mês corrente; senão, do mês seguinte. Dias 29–31 são clamped ao último dia
   do mês. A fatura é criada sob demanda (find-or-create; índice único
   `CardId + YearMonth` já existia).

2. **Parcelas = compras independentes agrupadas** (mantém ADR-001). Declarar
   "parcelado em N" (`DeclareInstallmentsUseCase`, só para compras `Pending`)
   converte a original na parcela 1/N e cria N−1 novas compras `Pending`,
   uma por mês (`Date.AddMonths`), cada uma na fatura correspondente,
   `DedupHash = null`. Agrupamento é só metadado:
   `Purchase.InstallmentGroupId / InstallmentNumber / TotalInstallments`.

3. **Divisão replicada.** Classificar qualquer parcela
   (`SplitPurchaseUseCase`/`ClassifyPurchaseUseCase`) replica a proporção das
   cotas para as irmãs ainda `Pending`
   (`InstallmentShareReplicator`), corrigindo drift de arredondamento na maior
   cota. Parcelas já classificadas manualmente não são sobrescritas.

4. **Lançamento manual.** `CreateManualPurchaseUseCase` cria compras
   `Pending` com data livre (passado permitido), comerciante opcional
   (find-or-create) e parcelamento opcional no ato.

5. **Visualização.** Nova aba "Faturas": lista por cartão com total/status e
   vencimento estimado (`DueDay` aplicado ao mês do fechamento ou ao seguinte,
   conforme ordem entre `DueDay` e `ClosingDay`).

Parsers **não** tentam detectar parcela no texto da notificação —
parcelamento é sempre declaração explícita do usuário.

## Consequências

- Migrações `AddPurchaseInstallmentFields` e `AddStatementDates` (esta última
  corrige lacuna antiga: `OpeningDate`/`ClosingDate` de `Statement` não eram
  mapeadas pelo EF por serem get-only; viraram `{ get; private set; }`).
- Compras futuras aparecem em "Pendentes" junto com as atuais — intencional,
  para dividir antes de entrar na fatura.
- Fechar/pagar fatura continua manual (`Statement.Close/MarkPaid`).
