using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Policies;

public sealed record PolicySet
{
    public PolicyVersion Version { get; }

    public RequestPolicy Request { get; }

    public OwnerPolicy Owner { get; }

    public HoldPolicy Hold { get; }

    public PolicySet(
        PolicyVersion version,
        RequestPolicy request,
        OwnerPolicy owner,
        HoldPolicy hold)
    {
        Version = version;
        Request = request
            ?? throw new ArgumentNullException(nameof(request));

        Owner = owner
            ?? throw new ArgumentNullException(nameof(owner));

        Hold = hold
            ?? throw new ArgumentNullException(nameof(hold));
    }
}