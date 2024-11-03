using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SocketMemory.Models;
public class Monitor{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private Dictionary<Socket, STATUS> _clientStatus;
    private bool _disposed;
    private int _clients;
    public Monitor()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = _loggerFactory.CreateLogger("Monitor manager");
        _clients = 0;
        _clientStatus = new();
        _disposed = false;
        Thread th = new (ShowData);
        th.Start();
    }
    public async void ShowData(){
        while(!_disposed){
            _logger.LogInformation($"Current online clients: {_clients}");
            foreach(var client in _clientStatus.Keys){
                var status = _clientStatus[client];
                var remoteAddress = client.RemoteEndPoint as IPEndPoint;
                _logger.LogInformation($"Client: {remoteAddress?.Address}:{remoteAddress?.Port} is {status}");
            }
            await Task.Delay(1000);
        }
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
    public bool IsActive(Socket client){
        try{
            return _clientStatus.ContainsKey(client) && (_clientStatus[client] == STATUS.EXECUTING || _clientStatus[client] == STATUS.IN_PROGRES);
        }catch(Exception ex){
            _logger.LogWarning(ex.Message);
        }
        return false;
    }
    public async Task<bool> RemoveClient(Socket id){
        try{
            if(_clientStatus.ContainsKey(id)){
                var status = _clientStatus[id];
                _logger.LogInformation("Removing socket client");
                if(status == STATUS.DISCONNECTING && id.Connected){
                    await id.DisconnectAsync(false);
                    _logger.LogInformation("Disconnecting client");

                }
                _clients--;
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