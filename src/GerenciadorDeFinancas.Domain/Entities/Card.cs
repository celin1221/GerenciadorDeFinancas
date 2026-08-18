using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Domain.Entities;

public sealed class Card
{
    public Guid Id { get; }

    public string Name { get; private set; } = null!;

    public string BankId { get; private set; } = null!;

    public string? Last4Digits { get; private set; }

    public Guid OwnerPersonId { get; private set; }

    public Person Owner { get; private set; } = null!;

    public int ClosingDay { get; private set; }

    public int DueDay { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; }

    private Card()
    {
    }

    public Card(
        string name,
        string bankId,
        string? last4Digits,
        Guid ownerPersonId,
        int closingDay,
        int dueDay)
    {
        Id = Guid.NewGuid();
        SetName(name);
        SetBank(bankId);
        SetLast4Digits(last4Digits);
        SetOwner(ownerPersonId);
        SetClosingDay(closingDay);
        SetDueDay(dueDay);
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Nome do cartão é obrigatório.");
        }

        Name = name.Trim();
    }

    public void SetBank(string bankId)
    {
        if (string.IsNullOrWhiteSpace(bankId))
        {
            throw new DomainException("Banco emissor é obrigatório.");
        }

        BankId = bankId.Trim().ToLowerInvariant();
    }

    public void SetLast4Digits(string? last4Digits)
    {
        if (last4Digits is not null && (last4Digits.Length != 4 || last4Digits.Any(c => !char.IsDigit(c))))
        {
            throw new DomainException("Os últimos 4 dígitos devem conter exatamente 4 números.");
        }

        Last4Digits = last4Digits;
    }

    public void SetOwner(Guid ownerPersonId) => OwnerPersonId = ownerPersonId;

    public void SetClosingDay(int day)
    {
        if (day is < 1 or > 31)
        {
            throw new DomainException("Dia de fechamento deve estar entre 1 e 31.");
        }

        ClosingDay = day;
    }

    public void SetDueDay(int day)
    {
        if (day is < 1 or > 31)
        {
            throw new DomainException("Dia de vencimento deve estar entre 1 e 31.");
        }

        DueDay = day;
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
