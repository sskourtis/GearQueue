using GearQueue.Protocol;
using GearQueue.UnitTests.Utils;

namespace GearQueue.UnitTests.Protocol;

public class UniqueIdTests
{
    [Fact]
    public void Create_WithCidOnly()
    {
        // Arrange
        var cid = RandomData.GetString(20);

        // Act
        var uniqueId = UniqueId.Create(cid, null);

        // Assert
        Assert.Equal(cid, uniqueId);
    }
    
    [Fact]
    public void Create_WithCidOnly_WhenItContainsSeparator()
    {
        // Arrange
        var cid = "Test|TestId";

        // Act
        var uniqueId = UniqueId.Create(cid, null);

        // Assert
        Assert.Equal("Test%7CTestId", uniqueId);
    }
    
    [Fact]
    public void Create_WithCidAndBatch()
    {
        // Arrange
        var cid = RandomData.GetString(20);
        var batchKey = RandomData.GetString(20);

        // Act
        var uniqueId = UniqueId.Create(cid, batchKey);

        // Assert
        Assert.Equal($"{cid}|{batchKey}", uniqueId);
    }
    
    [Fact]
    public void Create_WithCidAndBatch_WhenBothContainSeparator()
    {
        // Arrange
        var cid = "Test|123";
        var batchKey = "Batch|12";

        // Act
        var uniqueId = UniqueId.Create(cid, batchKey);

        // Assert
        Assert.Equal("Test%7C123|Batch%7C12", uniqueId);
    }
    
    [Fact]
    public void Decode_WithCidOnly()
    {
        // Arrange
        var uniqueId = RandomData.GetString(20);

        // Act
        var (cid, batch) = UniqueId.Decode(uniqueId);

        // Assert
        Assert.Equal(uniqueId, cid);
        Assert.Null(batch);
    }
    
    [Fact]
    public void Decode_WithCidOnly_WhenItContainsSeparator()
    {
        // Arrange
        var uniqueId = "Test%7C1234";

        // Act
        var (cid, batch) = UniqueId.Decode(uniqueId);

        // Assert
        Assert.Equal("Test|1234", cid);
        Assert.Null(batch);
    }
    
    [Fact]
    public void Decode_WithCidAndBatch()
    {
        // Arrange
        var uniqueId = "testcid|testbatch";

        // Act
        var (cid, batch) = UniqueId.Decode(uniqueId);

        // Assert
        Assert.Equal("testcid", cid);
        Assert.Equal("testbatch", batch);
    }
    
    [Fact]
    public void Decode_WithCidAndBatchWhenBothContainSeparator()
    {
        // Arrange
        var uniqueId = "Test%7C123|Batch%7C12";

        // Act
        var (cid, batch) = UniqueId.Decode(uniqueId);

        // Assert
        Assert.Equal("Test|123", cid);
        Assert.Equal("Batch|12", batch);
    }
}