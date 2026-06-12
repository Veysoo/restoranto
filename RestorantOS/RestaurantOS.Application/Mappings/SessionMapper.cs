using RestaurantOS.Application.DTOs.Sessions;
using RestaurantOS.Domain.Entities;

namespace RestaurantOS.Application.Mappings;

public static class SessionMapper
{
    public static SessionDetailDto ToDetailDto(Session session)
    {
        var paidAmount = session.Payments.Where(p => !p.IsVoid).Sum(p => p.Amount);
        return new SessionDetailDto
        {
            SessionId = session.SessionId,
            TableId = session.TableId,
            TableNumber = session.Table.TableNumber,
            TableName = session.Table.Name,
            OpenedAt = session.OpenedAt,
            GuestCount = session.GuestCount,
            Status = session.Status,
            TotalAmount = session.TotalAmount,
            DiscountAmount = session.DiscountAmount,
            TaxAmount = session.TaxAmount,
            FinalAmount = session.FinalAmount,
            PaidAmount = paidAmount,
            RemainingAmount = session.FinalAmount - paidAmount,
            RowVersion = session.RowVersion,
            OrderItems = session.OrderItems
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new OrderItemDto
                {
                    OrderItemId = i.OrderItemId,
                    MenuItemId = i.MenuItemId,
                    Name = i.MenuItem.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Discount = i.Discount,
                    LineTotal = i.LineTotal,
                    Status = i.Status,
                    Notes = i.Notes,
                    RowVersion = i.RowVersion
                }).ToList(),
            Payments = session.Payments.Select(p => new PaymentDto
            {
                PaymentId = p.PaymentId,
                Amount = p.Amount,
                Method = p.Method,
                PaidAt = p.PaidAt,
                ChangeGiven = p.ChangeGiven,
                IsVoid = p.IsVoid
            }).ToList()
        };
    }
}
