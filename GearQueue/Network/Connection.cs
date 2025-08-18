using System.Buffers;
using System.Net;
using System.Net.Sockets;
using GearQueue.Logging;
using GearQueue.Options;
using GearQueue.Protocol;
using GearQueue.Protocol.Request;
using GearQueue.Protocol.Response;
using Microsoft.Extensions.Logging;

namespace GearQueue.Network;

internal interface IConnection
{
    int Id { get; }
    Task Connect(CancellationToken cancellationToken = default);

    Task SendPacket(RequestPacket packet, CancellationToken cancellationToken = default);

    Task<ResponsePacket?> GetPacket(CancellationToken cancellationToken = default, bool skipBody = false);

    void Dispose();
}

internal class Connection : IDisposable, IConnection
{
    private static int _nextId = 0;
 
    
    private readonly ILogger<Connection> _logger;
    public int Id { get; }
    
    private Socket? _socket;
    private bool _disposed;
    
    private readonly GearQueueHostOptions _options;
    
    public Connection(ILoggerFactory loggerFactory, GearQueueHostOptions options)
    {
        _logger = loggerFactory.CreateLogger<Connection>();
        Id = Interlocked.Increment(ref _nextId);
        _options = options;
    }

    public async Task Connect(CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(_options.ConnectionTimeout);
        
        if (cancellationToken == CancellationToken.None)
        {
            await ConnectWithTimeout(timeoutCts.Token).ConfigureAwait(false);
            return;
        }
        
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        await ConnectWithTimeout(linkedCts.Token).ConfigureAwait(false);
    }
    
    private async Task ConnectWithTimeout(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Hostname))
            throw new ArgumentNullException(nameof(_options.Hostname));

        if (_options.Port is <= 0 or > 65535)
            throw new ArgumentException("port must be between 1 and 65535");
        
        IPAddress address;
			
        try 
        {
            address = IPAddress.Parse(_options.Hostname);
        } 
        catch (Exception)
        {
            var addresses = await Dns.GetHostAddressesAsync(_options.Hostname, cancellationToken).ConfigureAwait(false);
            
            // Connect to the first DNS entry
            address = addresses[Random.Shared.Next(addresses.Length)];	
        }

        cancellationToken.ThrowIfCancellationRequested();
			
        var endPoint = new IPEndPoint(address, _options.Port);
			 
        // dispose old object on reconnect
        _socket?.Dispose();
        _socket = new Socket(
            address.AddressFamily.Equals(AddressFamily.InterNetworkV6)
                ? AddressFamily.InterNetworkV6
                : AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);
        _socket.NoDelay = true;
			
        await _socket.ConnectAsync(endPoint, cancellationToken).ConfigureAwait(false);
        
        _logger.LogConnectionEstablished(_options.Hostname, _options.Port, Id);
    }

    public async Task SendPacket(RequestPacket packet, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(_options.SendTimeout);
        
        if (cancellationToken == CancellationToken.None)
        {
            await SendPacketWithTimeout(packet, timeoutCts.Token).ConfigureAwait(false);
            return;
        }
        
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        await SendPacketWithTimeout(packet, linkedCts.Token).ConfigureAwait(false);
    }
    
    private async Task SendPacketWithTimeout(RequestPacket packet, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_socket);
        ThrowIfDisposed();

        try
        {
            await SendAll(packet.FullData, cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            throw new InvalidOperationException("Connection is closed or disposed");
        }
        catch (OperationCanceledException e)
        {
            throw new InvalidOperationException($"Socket error occurred: {e.Message}", e); 
        }
    }
    
    private async Task SendAll(byte[] buffer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_socket);
        ThrowIfDisposed();
    
        var totalBytesSent = 0;
        while (totalBytesSent < buffer.Length)
        {
            var bytesSent = await _socket.SendAsync(
                buffer.AsMemory(totalBytesSent),
                SocketFlags.None,
                cancellationToken).ConfigureAwait(false);
        
            if (bytesSent == 0)
            {
                throw new SocketException((int)SocketError.ConnectionAborted);
            }
        
            totalBytesSent += bytesSent;
        }
    }

    public async Task<ResponsePacket?> GetPacket(CancellationToken cancellationToken = default, bool skipBody = false)
    {
        using var timeoutCts = new CancellationTokenSource(_options.ReceiveTimeout);
        
        if (cancellationToken == CancellationToken.None)
        {
            return await GetPacketWithTimeout(timeoutCts.Token, skipBody).ConfigureAwait(false);
        }
        
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        return await GetPacketWithTimeout(linkedCts.Token).ConfigureAwait(false);
    }
    
    private async Task<ResponsePacket?> GetPacketWithTimeout(CancellationToken cancellationToken, bool skipBody = false)
    {
        ArgumentNullException.ThrowIfNull(_socket);
        ThrowIfDisposed();

        try
        {
            byte[]? headerBuffer = null;

            try
            {
                // Initialize to 12 bytes (header only), and resize later as needed
                headerBuffer = ArrayPool<byte>.Shared.Rent(12);

                await ReceiveExactAsync(headerBuffer, 12, cancellationToken).ConfigureAwait(false);

                // Extract type and length using optimized bit manipulation instead of Reverse() and BitConverter
                int type, length;

                if (BitConverter.IsLittleEndian)
                {
                    // Network byte order (big endian) to host byte order (little endian)
                    type = (headerBuffer[4] << 24) | (headerBuffer[5] << 16) | (headerBuffer[6] << 8) | headerBuffer[7];
                    length = (headerBuffer[8] << 24) | (headerBuffer[9] << 16) | (headerBuffer[10] << 8) | headerBuffer[11];
                }
                else
                {
                    // Already in the right format on big-endian systems
                    type = BitConverter.ToInt32(headerBuffer.AsSpan(4, 4));
                    length = BitConverter.ToInt32(headerBuffer.AsSpan(8, 4));
                }

                if (length == 0)
                {
                    return new ResponsePacket
                    {
                        Type = (PacketType)type,
                        Data = []
                    };
                }

                if (skipBody)
                {
                    byte[]? bodyBuffer = null;
                    try
                    {
                        bodyBuffer = ArrayPool<byte>.Shared.Rent(length);
                        await ReceiveExactAsync(bodyBuffer, length, cancellationToken).ConfigureAwait(false);
                        
                        return new ResponsePacket
                        {
                            Type = (PacketType)type,
                            Data = [],
                        };
                    }
                    finally
                    {
                        if (bodyBuffer != null)
                        {
                            ArrayPool<byte>.Shared.Return(bodyBuffer);    
                        }
                    }
                }

                var data = new byte[length];

                await ReceiveExactAsync(data, length, cancellationToken).ConfigureAwait(false);

                return new ResponsePacket
                {
                    Type = (PacketType)type,
                    Data = data,
                };
            }
            finally
            {
                if (headerBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(headerBuffer);
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // Object disposed, probably shutdown
        }
        catch (OperationCanceledException)
        {
            // Operation was canceled, just return
        }

        return null;
    }
    
    private async Task ReceiveExactAsync(byte[] buffer, int length, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(_socket);
        ThrowIfDisposed();
        
        var totalRead = 0;

        while (totalRead < length)
        {
            var received = await _socket.ReceiveAsync(
                buffer.AsMemory(totalRead, length - totalRead),
                SocketFlags.None,
                cancellationToken).ConfigureAwait(false);

            if (received == 0)
            {
                throw new SocketException((int)SocketError.ConnectionAborted);
            }

            totalRead += received;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
            
        lock (this)
        {
            if (_disposed)
                return;
                
            try
            {
                _socket?.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                // Ignore errors during shutdown
            }
            
            _socket?.Close();
            _socket?.Dispose();
            _socket = null;
            
            _logger.LogConnectionClosed(Id);
            
            _disposed = true;
        }
    }
    
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Connection));
        }
    }
}