namespace RestaurantOS.Domain.Entities;

public class Table : BaseEntity
{
    public Guid TableId { get; set; } = Guid.NewGuid();
    public int TableNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Section { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
