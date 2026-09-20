namespace AllocationEngine.Domain.Policies;

public readonly record struct PolicyEvaluationResult
{
    public bool IsAllowed { get; }

    public PolicyRejection? Rejection { get; }

    private PolicyEvaluationResult(
        bool isAllowed,
        PolicyRejection? rejection)
    {
        IsAllowed = isAllowed;
        Rejection = rejection;
    }

    public static PolicyEvaluationResult Allowed()
    {
        return new(true, null);
    }

    public static PolicyEvaluationResult Rejected(
        PolicyRejection rejection)
    {
        return new(false, rejection);
    }
}
