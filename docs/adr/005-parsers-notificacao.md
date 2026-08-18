# ADR-005: Parser de notificações com registry e fallback genérico

- Status: Aceito
- Data: 2026-08-05

## Contexto

Cada banco formata suas notificações de forma diferente. O app precisa
suportar múltiplos bancos sem acoplamento e com baixo custo de adição.

## Decisão

- Abstração `INotificationParser` com `CanHandle(notification)` + `TryParse(notification)`.
- `NotificationParserRegistry` seleciona o parser de maior prioridade que
  aceite a notificação (package do app emissor via `KnownBanks`).
- Parsers v1 (stubs, `TryParse` retorna `null` por enquanto): **Nubank**,
  **Mercado Pago**, **Inter**, **Banco do Brasil**.
- `GenericNotificationParser` como fallback: aceita qualquer notificação
  (`CanHandle` = true, prioridade mais baixa) para não descartar nada.
- Resultado de importação: `Unsupported`, `ParseFailed`, `Duplicate`,
  `CardNotMatched` ou `Created`.

## Consequências

- Adicionar banco = nova classe `INotificationParser` + registro no DI.
- Stubs já entregam o esqueleto; `TryParse` real será implementado na Fase 1
  com base nas notificações reais dos bancos.
