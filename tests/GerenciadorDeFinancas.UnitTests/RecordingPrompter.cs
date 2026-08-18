using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.Ports;

namespace GerenciadorDeFinancas.UnitTests;

public sealed class RecordingPrompter : IClassificationPrompter
{
    public List<ClassificationPrompt> Prompts { get; } = new();

    public void Prompt(ClassificationPrompt prompt) => Prompts.Add(prompt);
}
