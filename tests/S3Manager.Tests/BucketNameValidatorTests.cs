using S3Manager.Web.Services;

namespace S3Manager.Tests;

public class BucketNameValidatorTests
{
    private readonly IBucketNameValidator _validator = new BucketNameValidator();

    [Fact]
    public void Validate_ValidName_ReturnsSuccess()
    {
        var (ok, error) = _validator.Validate("my-test-bucket");
        Assert.True(ok);
        Assert.Null(error);
    }

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        var (ok, error) = _validator.Validate("");
        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_TooShort_ReturnsError()
    {
        var (ok, error) = _validator.Validate("ab");
        Assert.False(ok);
    }

    [Fact]
    public void Validate_TooLong_ReturnsError()
    {
        var (ok, error) = _validator.Validate(new string('a', 64));
        Assert.False(ok);
    }

    [Fact]
    public void Validate_IpLike_ReturnsError()
    {
        var (ok, error) = _validator.Validate("192.168.1.1");
        Assert.False(ok);
    }

    [Fact]
    public void Validate_ReservedPrefix_ReturnsError()
    {
        var (ok, error) = _validator.Validate("xn--bucket");
        Assert.False(ok);
    }
}
