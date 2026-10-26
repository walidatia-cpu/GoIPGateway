namespace GoIPGateway.Domain.Common;

public abstract class BaseEntity<TKey>
{
    public TKey Id { get; set; } = default!;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? CreatedById { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedById { get; set; }
    public bool IsDelete { get; set; } = false;
}

public abstract class BaseEntity : BaseEntity<int>
{
}

public interface ISoftDelete
{
    bool IsDelete { get; set; }
}

public interface IAuditable
{
    DateTime CreatedDate { get; set; }
    string? CreatedById { get; set; }
    DateTime? ModifiedDate { get; set; }
    string? ModifiedById { get; set; }
}
