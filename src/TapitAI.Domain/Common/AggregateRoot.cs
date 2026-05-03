using TapitAI.Domain.Events;

namespace TapitAI.Domain.Common;

public abstract class AggregateRoot : AuditableEntity, ISoftDeletable
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
