# ADR-001: Parcelas como compras independentes

- Status: Aceito
- Data: 2026-08-05

## Contexto

Notificações de cartão de crédito frequentemente indicam compras parceladas
("Você fez uma compra de R$ 300,00 em 3x de R$ 100,00"). Para gerenciar
despesas compartilhadas é preciso decidir como representar parcelas.

## Decisão

Cada parcela é representada como uma **compra independente** (um registro
`Purchase` próprio, com data e valor da parcela). A compra original e a
continuidade do parcelamento ficam registradas apenas via `BankRefId` quando o
banco fornece identificadores consistentes.

## Consequências

- Simples de modelar e de dividir entre pessoas (cada parcela pode ter divisão própria).
- Não há agregação de parcelas; a fatura agrupa por data (statement).
- Perde-se a ligação semântica "parcelas da mesma compra" (sem campo `ParentPurchaseId`).
  Reavaliar no futuro se a classificação/relatórios exigirem.
