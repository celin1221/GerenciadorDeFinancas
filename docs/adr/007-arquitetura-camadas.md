# ADR-007: Arquitetura limpa e direção de dependências

- Status: Aceito
- Data: 2026-08-05

## Contexto

App de tamanho pequeno-médio, mas com regras de negócio (divisões, fatura,
classificação) que precisam de isolamento, testabilidade e evolução modular.

## Decisão

Estrutura em camadas em `src/`:

| Projeto                    | Responsabilidade                                        |
| -------------------------- | ------------------------------------------------------- |
| `GerenciadorDeFinancas.Domain` | Entidades, value objects, enums, exceções, abstrações de repositório e `IUnitOfWork` |
| `GerenciadorDeFinancas.Application` | DTOs, portas (parser/prompter), casos de uso       |
| `GerenciadorDeFinancas.Infrastructure` | Implementações de parser de notificações, helpers  |
| `GerenciadorDeFinancas.Persistence` | EF Core, `FinanceDbContext`, repositórios, `DbInitializer` |
| `GerenciadorDeFinancas.Mobile` | MAUI: composição DI, UI, serviços de plataforma   |
| `tests/GerenciadorDeFinancas.UnitTests` | Testes xUnit                |

Regra: dependências apontam para dentro — `Domain` não referencia nada;
`Application` e `Persistence` dependem de `Domain`; `Infrastructure` depende de
`Application`; `Mobile` referencia todos e **compõe o DI** (`MauiProgram`).

## Consequências

- Regras de negócio vivem no `Domain` e casos de uso no `Application`,
  independentes de UI/persistência.
- Repositórios são implementações de abstrações de `Domain`; `IUnitOfWork`
  orquestra transações.
- A troca de SQLite por outro storage não afeta `Application`.
- Custo inicial de boilerplate maior, compensado por testabilidade.
