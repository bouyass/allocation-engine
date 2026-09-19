using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Tests.ValueObjects;

public class ResourceIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_CreatesResourceId()
    {
        var value = Guid.NewGuid();

        var id = new ResourceId(value);

        Assert.Equal(value, id.Value);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new ResourceId(Guid.Empty));
    }

    [Fact]
    public void ResourceIds_WithSameValue_AreEqual()
    {
        var value = Guid.NewGuid();

        Assert.Equal(
            new ResourceId(value),
            new ResourceId(value));
    }

    [Fact]
    public void ResourceIds_WithDifferentValues_AreNotEqual()
    {
        Assert.NotEqual(
            new ResourceId(Guid.NewGuid()),
            new ResourceId(Guid.NewGuid()));
    }
}