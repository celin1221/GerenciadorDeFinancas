using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Enums;

namespace GerenciadorDeFinancas.Application.Services;

public static class InstallmentShareReplicator
{
    public static async Task ReplicateAsync(
        IUnitOfWork unitOfWork,
        Purchase classifiedPurchase,
        CancellationToken cancellationToken = default)
    {
        if (classifiedPurchase.InstallmentGroupId is not { } groupId)
        {
            return;
        }

        var siblings = (await unitOfWork.Purchases.ListByGroupAsync(groupId, cancellationToken))
            .Where(purchase => purchase.Id != classifiedPurchase.Id && purchase.Status == PurchaseStatus.Pending)
            .ToList();

        foreach (var sibling in siblings)
        {
            var shares = ScaleShares(classifiedPurchase.Shares, classifiedPurchase.AmountCents, sibling.AmountCents);
            sibling.SetShares(shares);
            sibling.MarkClassified();
            foreach (var share in sibling.Shares)
            {
                unitOfWork.Purchases.AddShare(share);
            }
        }
    }

    public static IReadOnlyList<(Guid PersonId, long AmountCents)> ScaleShares(
        IReadOnlyCollection<PurchaseShare> sourceShares,
        long sourceTotal,
        long targetTotal)
    {
        var scaled = sourceShares
            .Select(share => (share.PersonId, share.AmountCents * targetTotal / sourceTotal))
            .ToList();

        var drift = targetTotal - scaled.Sum(share => share.Item2);
        if (drift != 0)
        {
            var largestIndex = 0;
            for (var i = 1; i < scaled.Count; i++)
            {
                if (scaled[i].Item2 > scaled[largestIndex].Item2)
                {
                    largestIndex = i;
                }
            }

            scaled[largestIndex] = (scaled[largestIndex].PersonId, scaled[largestIndex].Item2 + drift);
        }

        return scaled;
    }
}
