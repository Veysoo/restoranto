namespace RestaurantOS.Domain.Entities;

public abstract class BaseEntity
{
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
