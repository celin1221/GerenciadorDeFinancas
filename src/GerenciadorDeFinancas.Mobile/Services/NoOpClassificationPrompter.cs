using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.Ports;

namespace GerenciadorDeFinancas.Services;

public sealed class NoOpClassificationPrompter : IClassificationPrompter
{
    public void Prompt(ClassificationPrompt prompt)
    {
    }
}
