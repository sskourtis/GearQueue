using System.Security.Cryptography;
using GearQueue.Protocol;
using GearQueue.Protocol.Request;

namespace GearQueue.UnitTests.Protocol;

public class RequestFactoryTests
{
     private static readonly byte[] ReqMagicBytes = "\0REQ"u8.ToArray();
    
    [Fact]
    public void Echo_IsCorrect()
    {
        // Assert
        var randomBytes = RandomNumberGenerator.GetBytes(10);
        
        // Act
        var requestPacket = RequestFactory.Echo(randomBytes);
        
        // Assert
        requestPacket.FullData.AsSpan();
        Assert.Equal(ReqMagicBytes, requestPacket.FullData.AsSpan(0, 4));
        Assert.Equal(new byte[]{0,0,0,0x10}, requestPacket.FullData.AsSpan(4, 4));
        Assert.Equal(new byte[]{0,0,0,0x0A}, requestPacket.FullData.AsSpan(8, 4));
        Assert.Equal(randomBytes, requestPacket.FullData.AsSpan(12));;
    }
    
    [Fact]
    public void GetStatus_IsCorrect()
    {
        // Assert
        var handle = "Test";
        
        // Act
        var requestPacket = RequestFactory.GetStatus(handle);
        
        // Assert
        requestPacket.FullData.AsSpan();
        Assert.Equal(ReqMagicBytes, requestPacket.FullData.AsSpan(0, 4));
        Assert.Equal(new byte[]{0,0,0,0x0f}, requestPacket.FullData.AsSpan(4, 4));
        Assert.Equal(new byte[]{0,0,0,0x04}, requestPacket.FullData.AsSpan(8, 4));
        Assert.Equal("Test"u8.ToArray(), requestPacket.FullData.AsSpan(12));;
    }
    
    [Fact]
    public void GetStatusUnque_IsCorrect()
    {
        // Assert
        var uniqueId = "Test2";
        
        // Act
        var requestPacket = RequestFactory.GetStatusUnique(uniqueId);
        
        // Assert
        requestPacket.FullData.AsSpan();
        Assert.Equal(ReqMagicBytes, requestPacket.FullData.AsSpan(0, 4));
        Assert.Equal(new byte[]{0,0,0,0x29}, requestPacket.FullData.AsSpan(4, 4));
        Assert.Equal(new byte[]{0,0,0,0x05}, requestPacket.FullData.AsSpan(8, 4));
        Assert.Equal("Test2"u8.ToArray(), requestPacket.FullData.AsSpan(12));;
    }
    
    [Fact]
    public void CanDo_IsCorrect()
    {
        // Assert
        var function = "test-function";
        
        // Act
        var requestPacket = RequestFactory.CanDo(function);
        
        // Assert
        requestPacket.FullData.AsSpan();
        Assert.Equal(ReqMagicBytes, requestPacket.FullData.AsSpan(0, 4));
        Assert.Equal(new byte[]{0,0,0,0x01}, requestPacket.FullData.AsSpan(4, 4));
        Assert.Equal(new byte[]{0,0,0,(byte)function.Length}, requestPacket.FullData.AsSpan(8, 4));
        Assert.Equal("test-function"u8.ToArray(), requestPacket.FullData.AsSpan(12));;
    }
    
    [Fact]
    public void CantDo_IsCorrect()
    {
        // Assert
        var function = "test-function";
        
        // Act
        var requestPacket = RequestFactory.CantDo(function);
        
        // Assert
        requestPacket.FullData.AsSpan();
        Assert.Equal(ReqMagicBytes, requestPacket.FullData.AsSpan(0, 4));
        Assert.Equal(new byte[]{0,0,0,0x02}, requestPacket.FullData.AsSpan(4, 4));
        Assert.Equal(new byte[]{0,0,0,(byte)function.Length}, requestPacket.FullData.AsSpan(8, 4));
        Assert.Equal("test-function"u8.ToArray(), requestPacket.FullData.AsSpan(12));;
    }
    
    [Fact]
    public void ResetAbilities_IsCorrect()
    {
        // Act
        var requestPacket = RequestFactory.ResetAbilities();
        
        // Assert
        requestPacket.FullData.AsSpan();
        Assert.Equal(ReqMagicBytes, requestPacket.FullData.AsSpan(0, 4));
        Assert.Equal(new byte[]{0,0,0,0x03}, requestPacket.FullData.AsSpan(4, 4));
        Assert.Equal(new byte[]{0,0,0,0}, requestPacket.FullData.AsSpan(8, 4));
    }
    
    [Fact]
    public void PreSleep_IsCorrect()
    {
        // Act
        var requestPacket = RequestFactory.PreSleep();
        
        // Assert
        requestPacket.FullData.AsSpan();
        Assert.Equal(ReqMagicBytes, requestPacket.FullData.AsSpan(0, 4));
        Assert.Equal(new byte[]{0,0,0,0x04}, requestPacket.FullData.AsSpan(4, 4));
        Assert.Equal(new byte[]{0,0,0,0}, requestPacket.FullData.AsSpan(8, 4));
    }
    
    [Fact]
    public void GrabJob_IsCorrect()
    {
        // Act
        var requestPacket = RequestFactory.GrabJob();
        
        // Assert
        requestPacket.FullData.AsSpan();
        Assert.Equal(ReqMagicBytes, requestPacket.FullData.AsSpan(0, 4));
        Assert.Equal(new byte[]{0,0,0,0x09}, requestPacket.FullData.AsSpan(4, 4));
        Assert.Equal(new byte[]{0,0,0,0}, requestPacket.FullData.AsSpan(8, 4));
    }
    
    [Fact]
    public void GrabJobUnique_IsCorrect()
    {
        // Act
        var requestPacket = RequestFactory.GrabJobUniq();
        
        // Assert
        requestPacket.FullData.AsSpan();
        Assert.Equal(ReqMagicBytes, requestPacket.FullData.AsSpan(0, 4));
        Assert.Equal(new byte[]{0,0,0,0x1e}, requestPacket.FullData.AsSpan(4, 4));
        Assert.Equal(new byte[]{0,0,0,0}, requestPacket.FullData.AsSpan(8, 4));
    }
    
    [Fact]
    public void GrabJobAll_IsCorrect()
    {
        // Act
        var requestPacket = RequestFactory.GrabJobAll();
        
        // Assert
        requestPacket.FullData.AsSpan();
        Assert.Equal(ReqMagicBytes, requestPacket.FullData.AsSpan(0, 4));
        Assert.Equal(new byte[]{0,0,0,0x27}, requestPacket.FullData.AsSpan(4, 4));
        Assert.Equal(new byte[]{0,0,0,0}, requestPacket.FullData.AsSpan(8, 4));
    }
    
    [Fact]
    public void WorkComplete_IsCorrect_WhenCalledWithoutData()
    {
        var handle = "Test";
        
        // Act
        var requestPacket = RequestFactory.WorkComplete(handle);
        
        // Assert
        requestPacket.FullData.AsSpan();
        Assert.Equal(ReqMagicBytes, requestPacket.FullData.AsSpan(0, 4));
        Assert.Equal(new byte[]{0,0,0,0x0d}, requestPacket.FullData.AsSpan(4, 4));
        Assert.Equal(new byte[]{0,0,0, (byte)"Test\0"u8.ToArray().Length}, requestPacket.FullData.AsSpan(8, 4));
        Assert.Equal("Test\0"u8.ToArray(), requestPacket.FullData.AsSpan(12));;
    }
    
    [Fact]
    public void WorkComplete_IsCorrect_WhenCalledWithData()
    {
        // Act
        var requestPacket = RequestFactory.WorkComplete("Test", "Ok"u8.ToArray());
        
        // Assert
        requestPacket.FullData.AsSpan();
        Assert.Equal(ReqMagicBytes, requestPacket.FullData.AsSpan(0, 4));
        Assert.Equal(new byte[]{0,0,0,0x0d}, requestPacket.FullData.AsSpan(4, 4));
        Assert.Equal(new byte[]{0,0,0, (byte)"Test\0Ok"u8.ToArray().Length}, requestPacket.FullData.AsSpan(8, 4));
        Assert.Equal("Test\0Ok"u8.ToArray(), requestPacket.FullData.AsSpan(12));;
    }
    
    [Fact]
    public void WorkFail_IsCorrect()
    {
        var handle = "Test";
        
        // Act
        var requestPacket = RequestFactory.WorkFail(handle);
        
        // Assert
        requestPacket.FullData.AsSpan();
        Assert.Equal(ReqMagicBytes, requestPacket.FullData.AsSpan(0, 4));
        Assert.Equal(new byte[]{0,0,0,0x0e}, requestPacket.FullData.AsSpan(4, 4));
        Assert.Equal(new byte[]{0,0,0, (byte)"Test"u8.ToArray().Length}, requestPacket.FullData.AsSpan(8, 4));
        Assert.Equal("Test"u8.ToArray(), requestPacket.FullData.AsSpan(12));;
    }
    
    [InlineData(JobPriority.High, true, 32)]
    [InlineData(JobPriority.High, false, 21)]
    [InlineData(JobPriority.Normal, true, 18)]
    [InlineData(JobPriority.Normal, false, 7)]
    [InlineData(JobPriority.Low, true, 34)]
    [InlineData(JobPriority.Low, false, 33)]
    [Theory]
    public void SubmitJob_IsCorrect(JobPriority priority, bool isBackground, byte binaryType)
    {
        // Act
        var requestPacket = RequestFactory.SubmitJob(
            "test-function",
            "very-unique-id",
            "very important job data"u8.ToArray(), 
            priority,
            isBackground);
        
        // Assert
        var expectedPayload = "test-function\0very-unique-id\0very important job data"u8.ToArray();
        
        requestPacket.FullData.AsSpan();
        Assert.Equal(ReqMagicBytes, requestPacket.FullData.AsSpan(0, 4));
        Assert.Equal(new byte[]{0,0,0,binaryType}, requestPacket.FullData.AsSpan(4, 4));
        Assert.Equal(new byte[]{0,0,0, (byte)expectedPayload.Length}, requestPacket.FullData.AsSpan(8, 4));
        Assert.Equal(expectedPayload, requestPacket.FullData.AsSpan(12));;
    }
}