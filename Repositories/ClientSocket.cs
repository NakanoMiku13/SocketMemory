using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using SocketMemory.Models;
using Microsoft.Extensions.Logging;
namespace SocketMemory.Repositories;
public class ClientSocket{
    private readonly Socket _socket;
    private readonly ILogger _logger;
    private readonly IPEndPoint _endpoint;
    private bool _isConnected; 
    private readonly CancellationToken _cancellationToken;
    private readonly ILoggerFactory _loggerFactory;
    public ClientSocket(string ip, int port, CancellationToken cancellationToken)
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = _loggerFactory.CreateLogger("Client socket");
        IPAddress serverIpObj = IPAddress.Parse(ip);
        IPEndPoint endpoint = new(serverIpObj, port);
        _socket = new(
            endpoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp
        );
        _endpoint = endpoint;
        _isConnected = false;
        _cancellationToken = cancellationToken;
    }
    public ClientSocket(IPAddress ip, int port, CancellationToken cancellationToken)
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = _loggerFactory.CreateLogger("Client socket");
        IPEndPoint endpoint = new(ip, port);
        _socket = new(
            endpoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp
        );
        _endpoint = endpoint;
        _isConnected = false;
        _cancellationToken = cancellationToken;
    }
    public async Task<bool> Connect(){
        try{
            await _socket.ConnectAsync(_endpoint, _cancellationToken);
            _isConnected = true;
            var t = await this.SendMessageAndWaitResponse(Constants.CLIENT);
            _isConnected = t.Contains(Constants.ACK);
            return _isConnected;
        }catch(Exception ex){
            _logger.LogCritical($"Error trying to connect to the server: {ex.Message}");
        }
        return false;
    }
    public async Task<string> SendMessageAndWaitResponse(string message){
        if(_isConnected){
            var mBytes = Encoding.UTF8.GetBytes(message);
            _ = await _socket.SendAsync(mBytes, SocketFlags.None, _cancellationToken);
            var buffer = new byte[2048];
            var recieved = await _socket.ReceiveAsync(buffer, SocketFlags.None, _cancellationToken);
            var response = Encoding.UTF8.GetString(buffer, 0, recieved);
            return response.ToString();
        }else{
            return "";
        }
    }
    public async Task<string> WaitMessage(){
        if(_isConnected){
            var buffer = new byte[2048];
            var recieved = await _socket.ReceiveAsync(buffer, SocketFlags.None, _cancellationToken);
            var response = Encoding.UTF8.GetString(buffer, 0, recieved);
            return response.ToString();
        }else{
            return "";
        }
    }
    public async Task<string> SendMessage(string message){
        if(_isConnected){
            var mBytes = Encoding.UTF8.GetBytes(message);
            _ = await _socket.SendAsync(mBytes, SocketFlags.None, _cancellationToken);
            return "";
        }else{
            return "";
        }
    }
    public string GetId(){
        var id = this._socket.RemoteEndPoint as IPEndPoint;
        return id == null ? "" : $"{id.Address.ToString()}:{id.Port}";
    }
    public void Dispose(){
        _socket.Shutdown(SocketShutdown.Both);
    }
}