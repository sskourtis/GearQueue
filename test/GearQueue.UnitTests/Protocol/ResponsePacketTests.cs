using System.Text;
using GearQueue.Protocol;
using GearQueue.Protocol.Response;
using GearQueue.UnitTests.Utils;

namespace GearQueue.UnitTests.Protocol;

public class ResponsePacketTests
{
    [Fact]
    public void Error_IsValid()
    {
        // Assert
        var errorCode = RandomData.GetString(5);
        var errorMessage = RandomData.GetString(20);
        var packetData =  Encoding.UTF8.GetBytes($"{errorCode}\0{errorMessage}");
        
        // Act
        var error = Error.Create(packetData);
        
        // Assert
        Assert.Equal(errorCode, error.ErrorCode);
        Assert.Equal(errorMessage, error.ErrorText);
    }
    
    [Fact]
    public void JobCreated_IsValid()
    {
        // Assert
        var handle = $"job_handle_{RandomData.GetString(5)}";
        var packetData = Encoding.UTF8.GetBytes(handle);
        
        // Act
        var jobCreated = JobCreated.Create(packetData);
        
        // Assert
        Assert.Equal(handle, jobCreated.JobHandle);
    }
    
    [Fact]
    public void JobAssign_IsValid()
    {
        // Assert
        var handle = $"job_handle_{RandomData.GetString(5)}";
        var functionName = $"function_name_{RandomData.GetString(5)}";
        var payload = RandomData.GetRandomBytes(25);
        
        var packetData = Encoding.UTF8.GetBytes($"{handle}\0{functionName}\0").ToList();
        packetData.AddRange(payload);
        
        // Act
        var jobAssigned = JobAssign.Create(packetData.ToArray());
        
        // Assert
        Assert.Equal(handle, jobAssigned.JobHandle);
        Assert.Equal(functionName, jobAssigned.FunctionName);
        Assert.Equal(payload, jobAssigned.Data.ToArray());
    }
    
    [Fact]
    public void JobAssignUniq_IsValid()
    {
        // Assert
        var handle = $"job_handle_{RandomData.GetString(5)}";
        var functionName = $"function_name_{RandomData.GetString(5)}";
        var uniqueId = RandomData.GetString(30);
        var payload = RandomData.GetRandomBytes(25);
        
        var packetData = Encoding.UTF8.GetBytes($"{handle}\0{functionName}\0{uniqueId}\0").ToList();
        packetData.AddRange(payload);
        
        // Act
        var jobAssigned = JobAssign.CreateUniq(packetData.ToArray());
        
        // Assert
        Assert.Equal(handle, jobAssigned.JobHandle);
        Assert.Equal(functionName, jobAssigned.FunctionName);
        Assert.Equal(uniqueId, jobAssigned.CorrelationId);
        Assert.Equal(payload, jobAssigned.Data.ToArray());
    }
    
    [Fact]
    public void JobAssignUniq_IsValid_WithBatchKey()
    {
        // Assert
        
        var handle = $"job_handle_{RandomData.GetString(5)}";
        var functionName = $"function_name_{RandomData.GetString(5)}";
        var cid = RandomData.GetString(30);
        var batchKey = RandomData.GetString(5);
        var uniqueId = UniqueId.Create(cid, batchKey);
        var payload = RandomData.GetRandomBytes(25);
        
        var packetData = Encoding.UTF8.GetBytes($"{handle}\0{functionName}\0{uniqueId}\0").ToList();
        packetData.AddRange(payload);
        
        // Act
        var jobAssigned = JobAssign.CreateUniq(packetData.ToArray());
        
        // Assert
        Assert.Equal(handle, jobAssigned.JobHandle);
        Assert.Equal(functionName, jobAssigned.FunctionName);
        Assert.Equal(cid, jobAssigned.CorrelationId);
        Assert.Equal(batchKey, jobAssigned.BatchKey);
        Assert.Equal(payload, jobAssigned.Data.ToArray());
    }
    
    [Fact]
    public void JobAssignAll_IsValid()
    {
        // Assert
        var handle = $"job_handle_{RandomData.GetString(5)}";
        var functionName = $"function_name_{RandomData.GetString(5)}";
        var uniqueId = RandomData.GetString(30);
        var reducer = RandomData.GetString(15);
        var payload = RandomData.GetRandomBytes(25);
        
        var packetData = Encoding.UTF8.GetBytes($"{handle}\0{functionName}\0{uniqueId}\0{reducer}\0").ToList();
        packetData.AddRange(payload);
        
        // Act
        var jobAssigned = JobAssign.CreateAll(packetData.ToArray());
        
        // Assert
        Assert.Equal(handle, jobAssigned.JobHandle);
        Assert.Equal(functionName, jobAssigned.FunctionName);
        Assert.Equal(uniqueId, jobAssigned.CorrelationId);
        Assert.Equal(reducer, jobAssigned.Reducer);
        Assert.Equal(payload, jobAssigned.Data.ToArray());
    }
}