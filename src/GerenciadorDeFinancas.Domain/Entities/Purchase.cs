using GerenciadorDeFinancas.Domain.Enums;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Domain.Entities;

public sealed class Purchase
{
    private readonly List<PurchaseShare> _shares = new();

    public Guid Id { get; }

    public Guid CardId { get; }

    public Card? Card { get; private set; }

    public Guid? MerchantId { get; private set; }

    public Merchant? Merchant { get; private set; }

    public Guid? CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public Guid? StatementId { get; private set; }

    public Statement? Statement { get; private set; }

    public long AmountCents { get; private set; }

    public DateTime Date { get; private set; }

    public DateTime? PostingDate { get; private set; }

    public string Description { get; private set; } = null!;

    public string? BankRefId { get; private set; }

    public string? DedupHash { get; private set; }

    public string? RawNotificationText { get; private set; }

    public Guid? InstallmentGroupId { get; private set; }

    public int InstallmentNumber { get; private set; }

    public int TotalInstallments { get; private set; }

    public bool IsInstallment => InstallmentGroupId is not null;

    public PurchaseStatus Status { get; private set; }

    public DateTime? ClassifiedAt { get; private set; }

    public DateTime CreatedAt { get; }

    public IReadOnlyCollection<PurchaseShare> Shares => _shares;

    public long ClassifiedAmountCents => _shares.Sum(share => share.AmountCents);

    private Purchase()
    {
    }

    public Purchase(
        Guid cardId,
        long amountCents,
        DateTime date,
        string description,
        Guid? merchantId = null,
        Guid? categoryId = null,
        Guid? statementId = null,
        string? bankRefId = null,
        string? dedupHash = null,
        string? rawNotificationText = null,
        DateTime? postingDate = null,
        Guid? installmentGroupId = null,
        int installmentNumber = 0,
        int totalInstallments = 0)
    {
        if (amountCents < 0)
        {
            throw new DomainException("Valor da compra não pode ser negativo.");
        }

        if (installmentGroupId is not null &&
            (totalInstallments < 2 || installmentNumber is < 1 || installmentNumber > totalInstallments))
        {
            throw new DomainException("Informações de parcela inválidas.");
        }

        Id = Guid.NewGuid();
        CardId = cardId;
        AmountCents = amountCents;
        Date = date;
        Description = description;
        MerchantId = merchantId;
        CategoryId = categoryId;
        StatementId = statementId;
        BankRefId = bankRefId;
        DedupHash = dedupHash;
        RawNotificationText = rawNotificationText;
        PostingDate = postingDate;
        InstallmentGroupId = installmentGroupId;
        InstallmentNumber = installmentNumber;
        TotalInstallments = totalInstallments;
        Status = PurchaseStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void AssignToSingle(Guid personId)
    {
        SetShares(new[] { (personId, AmountCents) });
        MarkClassified();
    }

    public void ConvertToFirstInstallment(Guid groupId, int totalInstallments, long firstAmountCents)
    {
        if (Status != PurchaseStatus.Pending)
        {
            throw new DomainException("Apenas compras pendentes podem ser parceladas.");
        }

        if (_shares.Count > 0)
        {
            throw new DomainException("Compra já dividida não pode ser parcelada.");
        }

        if (InstallmentGroupId is not null)
        {
            throw new DomainException("Compra já pertence a um parcelamento.");
        }

        if (totalInstallments < 2)
        {
            throw new DomainException("Parcelamento precisa de ao menos 2 parcelas.");
        }

        if (firstAmountCents <= 0)
        {
            throw new DomainException("Valor da parcela deve ser maior que zero.");
        }

        AmountCents = firstAmountCents;
        InstallmentGroupId = groupId;
        InstallmentNumber = 1;
        TotalInstallments = totalInstallments;
    }

    public void SetShares(IReadOnlyList<(Guid PersonId, long AmountCents)> shares)
    {
        if (shares.Count == 0)
        {
            throw new DomainException("A compra precisa de ao menos uma participação.");
        }

        if (shares.Any(share => share.AmountCents <= 0))
        {
            throw new DomainException("Valor de participação deve ser maior que zero.");
        }

        if (shares.Sum(share => share.AmountCents) != AmountCents)
        {
            throw new DomainException("A soma das participações difere do valor da compra.");
        }

        if (shares.Select(share => share.PersonId).Distinct().Count() != shares.Count)
        {
            throw new DomainException("A mesma pessoa não pode ter participação duplicada.");
        }

        _shares.Clear();
        foreach (var share in shares)
        {
            _shares.Add(new PurchaseShare(this, share.PersonId, share.AmountCents));
        }
    }

    public void MarkClassified()
    {
        if (_shares.Count == 0 || _shares.Sum(share => share.AmountCents) != AmountCents)
        {
            throw new DomainException("A compra precisa estar totalmente dividida para ser classificada.");
        }

        Status = PurchaseStatus.Classified;
        ClassifiedAt = DateTime.UtcNow;
    }

    public void MarkIgnored()
    {
        _shares.Clear();
        Status = PurchaseStatus.Ignored;
        ClassifiedAt = DateTime.UtcNow;
    }

    public void SetCategory(Guid? categoryId) => CategoryId = categoryId;

    public void UpdateDetails(string? description, long? amountCents, DateTime? date)
    {
        if (description is not null)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new DomainException("Descrição da compra é obrigatória.");
            }

            Description = description.Trim();
        }

        if (amountCents is { } newAmount)
        {
            if (Status != PurchaseStatus.Pending || _shares.Count > 0)
            {
                throw new DomainException("O valor só pode ser alterado enquanto a compra está pendente.");
            }

            if (newAmount <= 0)
            {
                throw new DomainException("Valor da compra deve ser maior que zero.");
            }

            AmountCents = newAmount;
        }

        if (date is { } newDate)
        {
            Date = newDate;
        }
    }

    public void Reopen()
    {
        _shares.Clear();
        Status = PurchaseStatus.Pending;
        ClassifiedAt = null;
    }

    public void SetMerchant(Guid? merchantId) => MerchantId = merchantId;

    public void MoveToStatement(Guid? statementId) => StatementId = statementId;

    public void SetPostingDate(DateTime? postingDate) => PostingDate = postingDate;
}
