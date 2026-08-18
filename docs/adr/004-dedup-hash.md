# ADR-004: Deduplicação de compras por hash

- Status: Aceito
- Data: 2026-08-05

## Contexto

O mesmo gasto pode gerar múltiplas notificações (duplicadas, repetidas pelo
banco, ou reintroduzidas por reimportação). Sem dedup, o app criaria compras
duplicadas.

## Decisão

Cada `Purchase` recebe `DedupHash` = SHA-256 (hex minúsculo) do payload:

```
{BankId}|{Merchant.Normalize(merchant)}|{AmountCents}|{Date:yyyyMMdd}
```

Antes de criar, `ImportNotificationUseCase` consulta
`GetByDedupHashAsync`; se existir, retorna `Duplicate()` sem gravar.
Índice único em `Purchase.DedupHash` no banco (reforço a nível de storage).

## Consequências

- Notificações duplicadas não geram compras repetidas.
- Compra idêntica de mesmo valor/merchant/data no mesmo dia é considerada
  duplicada — falha conhecida para compras legítimas iguais no mesmo dia
  (aceitável nesta fase; reavaliar com `BankRefId`).
