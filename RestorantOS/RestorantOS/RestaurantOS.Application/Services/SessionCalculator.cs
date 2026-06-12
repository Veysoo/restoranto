using RestaurantOS.Domain.Entities;
using RestaurantOS.Domain.Enums;

namespace RestaurantOS.Application.Services;

public static class SessionCalculator
{
    public static void Recalculate(Session session)
    {
        var activeItems = session.OrderItems
            .Where(i => i.Status != OrderItemStatus.Cancelled)
            .ToList();

        session.TotalAmount = activeItems.Sum(i => i.LineTotal);

        session.TaxAmount = activeItems.Sum(i =>
        {
            var taxableBase = i.LineTotal - i.Discount;
            return Math.Round(taxableBase * (i.MenuItem?.TaxRate ?? 10m) / 100m, 2);
        });

        session.FinalAmount = session.TotalAmount - session.DiscountAmount + session.TaxAmount;
    }

    public static decimal CalculateLineTotal(int quantity, decimal unitPrice, decimal discount)
        => Math.Round(quantity * unitPrice - discount, 2);
}
