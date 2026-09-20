using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Holds;

public sealed class Hold
{
    public HoldId Id { get; }

    public ResourceId ResourceId { get; }

    public OwnerId OwnerId { get; }

    public Quantity Quantity { get; }

    public HoldStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset ExpiresAt { get; }

    public PolicyVersion PolicyVersion { get; }

    public Hold(
        HoldId id,
        ResourceId resourceId,
        OwnerId ownerId,
        Quantity quantity,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        PolicyVersion policyVersion)
    {
        if (quantity == Quantity.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Hold quantity must be greater than zero.");
        }

        if (expiresAt <= createdAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "Hold expiration must be after its creation.");
        }

        Id = id;
        ResourceId = resourceId;
        OwnerId = ownerId;
        Quantity = quantity;

        CreatedAt = createdAt;
        ExpiresAt = expiresAt;

        PolicyVersion = policyVersion;

        Status = HoldStatus.Held;
    }

    public bool IsReclaimable(DateTimeOffset now)
    {
        return Status == HoldStatus.Held
           && now >= ExpiresAt;
    }

    public void Confirm()
    {
        switch (Status)
        {
            case HoldStatus.Held:
                Status = HoldStatus.Confirmed;
                break;

            case HoldStatus.Confirmed:
                return;

            case HoldStatus.Released:
            case HoldStatus.Expired:
                throw new InvalidOperationException("Hold cannot be confirmed because it has been released or expired.");

            default:
                throw new InvalidOperationException($"Hold cannot be confirmed because it is in an invalid state: {Status}.");
        }
    }

    public void Release()
    {
        switch (Status)
        {
            case HoldStatus.Held:
            case HoldStatus.Confirmed:
                Status = HoldStatus.Released;
                break;

            case HoldStatus.Released:
            case HoldStatus.Expired:
                return;

            default:
                throw new InvalidOperationException($"Hold cannot be released because it is in an invalid state: {Status}.");
        }
    }

    public void Reclaim(DateTimeOffset now)
    {
        switch (Status)
        {
            case HoldStatus.Expired:
            case HoldStatus.Confirmed:
            case HoldStatus.Released:
                return;

            case HoldStatus.Held:
                if (now < ExpiresAt)
                {
                    throw new InvalidOperationException("Hold cannot be reclaimed because it has not yet expired.");
                }

                Status = HoldStatus.Expired;
                break;
        }
    }
}
