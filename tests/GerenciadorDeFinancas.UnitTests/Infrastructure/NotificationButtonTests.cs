using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.UnitTests.Infrastructure;

public class NotificationButtonTests
{
    [Fact]
    public void Constructor_CreatesButtonWithLabelAndOrder()
    {
        var button = new NotificationButton("JO/MA", 0);

        Assert.NotEqual(Guid.Empty, button.Id);
        Assert.Equal("JO/MA", button.Label);
        Assert.Equal(0, button.Order);
        Assert.Empty(button.Persons);
    }

    [Fact]
    public void SetLabel_ValidLabel_UpdatesLabel()
    {
        var button = new NotificationButton("JO", 0);
        button.SetLabel("JO/MA");
        Assert.Equal("JO/MA", button.Label);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetLabel_NullOrWhitespace_ThrowsDomainException(string? label)
    {
        var button = new NotificationButton("JO", 0);
        Assert.Throws<DomainException>(() => button.SetLabel(label!));
    }

    [Fact]
    public void SetLabel_TooLong_ThrowsDomainException()
    {
        var button = new NotificationButton("JO", 0);
        Assert.Throws<DomainException>(() => button.SetLabel(new string('A', 21)));
    }

    [Fact]
    public void SetPersons_ValidIds_CreatesPersons()
    {
        var button = new NotificationButton("JO/MA", 0);
        var person1 = Guid.NewGuid();
        var person2 = Guid.NewGuid();

        button.SetPersons(new[] { person1, person2 });

        Assert.Equal(2, button.Persons.Count);
        Assert.Contains(button.Persons, bp => bp.PersonId == person1);
        Assert.Contains(button.Persons, bp => bp.PersonId == person2);
    }

    [Fact]
    public void SetPersons_Empty_ThrowsDomainException()
    {
        var button = new NotificationButton("JO", 0);
        Assert.Throws<DomainException>(() => button.SetPersons(Array.Empty<Guid>()));
    }

    [Fact]
    public void SetPersons_Duplicates_Deduplicates()
    {
        var button = new NotificationButton("JO", 0);
        var personId = Guid.NewGuid();

        button.SetPersons(new[] { personId, personId });

        Assert.Single(button.Persons);
    }
}
