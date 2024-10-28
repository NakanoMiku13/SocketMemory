using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SocketMemory.Models;
public class Monitor{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private Dictionary<Socket, STATUS> _clientStatus;
    private int _clients;
    public Monitor()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = _loggerFactory.CreateLogger("Monitor manager");
        _clients = 0;
        _clientStatus = new();
    }
    public async Task ChangeClientStatus(Socket id, STATUS status){
        try{

            if(!_clientStatus.ContainsKey(id)) AddClient(id);
            else{
                if(status == STATUS.DISCONNECTING){
                    bool success = await RemoveClient(id);
                }
                var remoteAddress = id.RemoteEndPoint as IPEndPoint;
                _logger.LogInformation($"Client: {remoteAddress?.Address}:{remoteAddress?.Port} change from: {_clientStatus[id]} to {status}");
                _clientStatus[id] = status;
            }
        }catch(Exception ex){
            _logger.LogError(ex.Message);
        }
    }
    public async Task<bool> RemoveClient(Socket id){
        try{
            if(_clientStatus.ContainsKey(id)){
                var status = _clientStatus[id];
                _clientStatus.Remove(id);
                _logger.LogInformation("Removing socket client");
                if(status == STATUS.DISCONNECTING && id.Connected){
                    await id.DisconnectAsync(false);
                    _logger.LogInformation("Disconnecting client");
                }
                id?.Dispose();
            }else{
                _logger.LogWarning("Client not found");
            }
            return true;
        }catch(Exception ex){
            _logger.LogError(ex.Message);
        }
        return false;
    }
    public void AddClient(Socket client){
        try{
            if(_clientStatus.ContainsKey(client)){
                _logger.LogWarning("Client already registered");
            }else{
                _logger.LogInformation("Registering new client");
                _clients ++;
                _clientStatus.Add(client, STATUS.CONNECTING);
            }
        }catch(Exception ex){
            _logger.LogError(ex.Message);
        }
    }
}