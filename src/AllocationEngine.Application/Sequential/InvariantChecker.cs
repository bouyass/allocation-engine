using AllocationEngine.Domain.Holds;
using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Application.Sequential;

public static class InvariantChecker
{
    public static IReadOnlyList<InvariantViolation> Check(
        SequentialAllocationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var violations = new List<InvariantViolation>();

        foreach (var resource in state.Resources)
        {
            var resourceHolds = state.Holds
                .Where(hold => hold.ResourceId == resource.Id)
                .ToList();

            var actualHeldQuantity = resourceHolds
                .Where(hold => hold.Status == HoldStatus.Held)
                .Aggregate(
                    Quantity.Zero,
                    (total, hold) => total + hold.Quantity);

            var actualAllocatedQuantity = resourceHolds
                .Where(hold => hold.Status == HoldStatus.Confirmed)
                .Aggregate(
                    Quantity.Zero,
                    (total, hold) => total + hold.Quantity);

            if (resource.HeldQuantity != actualHeldQuantity)
            {
                violations.Add(
                    new InvariantViolation(
                        "RESOURCE_HELD_QUANTITY_MISMATCH",
                        $"Resource {resource.Id} reports held quantity " +
                        $"{resource.HeldQuantity}, but its HELD holds total " +
                        $"{actualHeldQuantity}."));
            }

            if (resource.AllocatedQuantity != actualAllocatedQuantity)
            {
                violations.Add(
                    new InvariantViolation(
                        "RESOURCE_ALLOCATED_QUANTITY_MISMATCH",
                        $"Resource {resource.Id} reports allocated quantity " +
                        $"{resource.AllocatedQuantity}, but its CONFIRMED holds total " +
                        $"{actualAllocatedQuantity}."));
            }

            if (resource.HeldQuantity + resource.AllocatedQuantity > resource.Capacity)
            {
                violations.Add(
                    new InvariantViolation(
                        "RESOURCE_CAPACITY_EXCEEDED",
                        $"Resource {resource.Id} has committed quantity " +
                        $"{resource.HeldQuantity + resource.AllocatedQuantity} " +
                        $"greater than capacity {resource.Capacity}."));
            }
        }

        foreach (var hold in state.Holds)
        {
            var resourceExists = state.Resources
                .Any(resource => resource.Id == hold.ResourceId);

            if (!resourceExists)
            {
                violations.Add(
                    new InvariantViolation(
                        "HOLD_REFERENCES_UNKNOWN_RESOURCE",
                        $"Hold {hold.Id} references resource {hold.ResourceId}, " +
                        "but that resource does not exist."));
            }
        }
        return violations;
    }
}
