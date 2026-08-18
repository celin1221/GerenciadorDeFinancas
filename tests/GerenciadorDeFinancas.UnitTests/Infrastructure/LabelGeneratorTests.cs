using GerenciadorDeFinancas.Application;

namespace GerenciadorDeFinancas.UnitTests.Infrastructure;

public class LabelGeneratorTests
{
    [Theory]
    [InlineData(new[] { "João" }, "JO")]
    [InlineData(new[] { "Maria" }, "MA")]
    [InlineData(new[] { "João", "Maria" }, "JO/MA")]
    [InlineData(new[] { "João", "Maria", "Pedro" }, "JO/MA/PE")]
    [InlineData(new[] { "Ana Clara", "João Pedro" }, "AN/JO")]
    public void Generate_ReturnsAbbreviatedLabel(string[] names, string expected)
    {
        var result = LabelGenerator.Generate(names);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Generate_EmptyList_ReturnsQuestionMark()
    {
        Assert.Equal("?", LabelGenerator.Generate(Array.Empty<string>()));
    }

    [Fact]
    public void Generate_ShortNames_UsesFullName()
    {
        var result = LabelGenerator.Generate(new[] { "Lu" });
        Assert.Equal("LU", result);
    }
}
