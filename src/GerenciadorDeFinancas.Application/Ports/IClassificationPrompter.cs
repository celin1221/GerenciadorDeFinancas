using GerenciadorDeFinancas.Application.Dtos;

namespace GerenciadorDeFinancas.Application.Ports;

public interface IClassificationPrompter
{
    void Prompt(ClassificationPrompt prompt);
}
