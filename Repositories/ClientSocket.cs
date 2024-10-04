using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
namespace SocketMemory.Repositories;
public class ClientSocket{
    private const string ACK = "|ACK|", CLIENT = "|CLIENT|";
    private readonly Socket _socket;
    private readonly ILogger _logger;
    private readonly IPEndPoint _endpoint;
    private bool _isConnected; 
    private readonly ILoggerFactory _loggerFactory;
    public ClientSocket(string ip, int port)
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
    }
    public async Task<bool> Connect(){
        try{
            await _socket.ConnectAsync(_endpoint);
            var t = await this.SendMessage(CLIENT);
            _isConnected = true;
            return _isConnected;
        }catch(Exception ex){
            _logger.LogCritical($"Error trying to connect to the server: {ex.Message}");
        }
        return false;
    }
    public async Task<string> SendMessage(string message){
        if(_isConnected){
            var mBytes = Encoding.UTF8.GetBytes(message);
            _ = await _socket.SendAsync(mBytes, SocketFlags.None);
            var buffer = new byte[2048];
            var recieved = await _socket.ReceiveAsync(buffer, SocketFlags.None);
            var response = Encoding.UTF8.GetString(buffer, 0, recieved);
            return response.ToString();
        }else{
            return "";
        }
    }
    public void Dispose(){
        _socket.Shutdown(SocketShutdown.Both);
    }
}