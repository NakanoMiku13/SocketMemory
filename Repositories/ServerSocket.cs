using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using SocketMemory.Models;
namespace SocketMemory.Repositories;
public class ServerSocket{
    
    private List<string> _words = new()
    {
        "amor", "avión", "árbol", "amigo", "bebé", "baño", "boca", "cielo", "cama", "coche",
        "casa", "carta", "ciudad", "conejo", "comida", "corazón", "cuadro", "dedo", "dormir", "dinero",
        "elefante", "espejo", "estrella", "escuela", "familia", "flor", "gato", "grillo", "hombre", "helado",
        "isla", "iglesia", "jardín", "jirafa", "jugador", "lámpara", "libro", "luna", "león", "madre",
        "mano", "música", "nieve", "niño", "nube", "naranja", "oso", "oveja", "ojo", "pájaro",
        "piedra", "perro", "playa", "pluma", "pelota", "papel", "puerta", "queso", "ratón", "reloj",
        "sol", "silla", "sombrero", "sonrisa", "tigre", "tijeras", "tren", "tortuga", "teléfono", "tierra",
        "uva", "viento", "ventana", "vaca", "zapato", "zanahoria", "agua", "aire", "bosque", "burro",
        "camino", "cerdo", "ciervo", "diamante", "dulce", "delfín", "enfermera", "escritorio", "estrella", "fuego",
        "fruta", "fuerte", "globo", "guitarra", "hermano", "hermana", "huevo", "iglu", "jirafa", "juego",
        "lago", "ladrón", "leyenda", "llave", "limón", "luz", "magia", "mapa", "mariposa", "menta",
        "miedo", "montaña", "muñeco", "nido", "nieve", "niña", "noche", "novela", "nube", "oído",
        "olvido", "oro", "pájaro", "palabra", "pantalón", "pared", "parque", "paseo", "pequeño", "pez",
        "pirata", "plato", "príncipe", "princesa", "profesor", "ratón", "regalo", "rey", "rica", "río",
        "rosa", "sal", "sabor", "sangre", "secreto", "serpiente", "sirena", "sombrero", "sonido", "sopa",
        "tabla", "techo", "tela", "templo", "tesoro", "torre", "trampa", "trueno", "tumba", "universo",
        "vampiro", "vaso", "vecino", "velero", "veneno", "viento", "vida", "volcán", "vuelo", "yegua",
        "zanahoria", "zapato", "zorro", "zoológico", "alga", "alondra", "anfibio", "asiento", "belleza", "beso",
        "blanco", "caballo", "caramelo", "castillo", "cuento", "duende", "esfinge", "hada", "historia", "lago",
        "laguna", "lámpara", "leyenda", "magia", "madera", "montaña", "nido", "paisaje", "perdido", "pesca",
        "piedra", "silencio", "sombra", "trampa", "viento", "volcán", "zapato"
    };
    private readonly string _ip;
    private readonly int _port;
    private readonly IPEndPoint _endpoint;
    private readonly Socket _socket;
    private List<Task> _clients;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly int _options = 3;
    private Dictionary<int, Thread> _rooms; 
    private Dictionary<int, List<Socket>> _roomClients;
    private Dictionary<int, Tuple<int,int>> _maxRoomClients;
    private readonly Monitor _monitor;
    public ServerSocket(string ip = "localhost", int port = 9595, int maxClients = 100)
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = _loggerFactory.CreateLogger("Client socket");
        _ip = ip;
        _port = port;
        _clients = new();
        _rooms = new();
        _roomClients = new();
        _endpoint = new(IPAddress.Parse(_ip), _port);
        _socket = new(
            _endpoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp
        );
        _socket.Bind(_endpoint);
        _socket.Listen(maxClients);
        _monitor = new();
        _maxRoomClients = new();
    }
    public ServerSocket(IPAddress ip, int port = 9595, int maxClients = 100)
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = _loggerFactory.CreateLogger("Server");
        _port = port;
        _endpoint = new(ip, _port);
        _ip = ip.ToString();
        _monitor = new();
        _socket = new(
            _endpoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp
        );
        _socket.Bind(_endpoint);
        _socket.Listen(maxClients);
        _clients = new();
        _roomClients = new();
        _rooms = new();
        _maxRoomClients = new();
    }
    private async Task<Socket?> AcceptClient(int timeout = 15){
        var client = _socket.AcceptAsync();
        var timeoutDelay = Task.Delay(timeout * 1000);
        if(await Task.WhenAny(client, timeoutDelay) == client){
            return await client;
        }else{
            return null;
        }
    }
    private async void NewClient(){
        var client = await AcceptClient();
        if(client != null){
            _monitor.AddClient(client);
            if(client.Connected){
                await _monitor.ChangeClientStatus(client, STATUS.CONNECTED);
                _clients.Add(StartConnection(client));
            }else{
                await _monitor.ChangeClientStatus(client, STATUS.FAIL);
            }
        }
    }   
    public async Task AcceptClientMainMenu(){
        Thread th = new(NewClient);
        th.Start();
    }
    public async Task CloseConnections(){
        await Task.WhenAll(_clients);
    }
    private async Task<string> GetMessage(Socket client){
        try{
            var buffer = new byte[2048];
            var recieved = await client.ReceiveAsync(buffer, SocketFlags.None);
            var response = Encoding.UTF8.GetString(buffer, 0, recieved);
            return response.ToString();
        }catch(Exception ex){
            _logger.LogError(ex.Message);
            return Constants.ERROR;
        }
    }
    private async Task<string> SendMessageAndWaitResponse(string message, Socket client){
        try{
            var mBytes = Encoding.UTF8.GetBytes(message);
            _ = await client.SendAsync(mBytes, SocketFlags.None);
            var buffer = new byte[2048];
            var recieved = await client.ReceiveAsync(buffer, SocketFlags.None);
            var response = Encoding.UTF8.GetString(buffer, 0, recieved);
            return response.ToString();
        }catch(Exception ex){
            _logger.LogError(ex.Message);
            return Constants.NACK;
        }
    }
    private async Task SendMessage(string message, Socket client){
        try{
            var mBytes = Encoding.UTF8.GetBytes(message);
            _ = await client.SendAsync(mBytes, SocketFlags.None);
            var buffer = new byte[2048];
        }catch(Exception ex){
            _logger.LogWarning(ex.Message);
        }
    }
    private async Task<string> NewSession(Socket client){
        bool notvalid = true;
        string response = "";
        int retries = 0;
        do{
            response = await SendMessageAndWaitResponse($"{Constants.MENU}-{Constants.OPTIONS}", client);
            if(response.Contains(Constants.OPTION)){
                int option = Convert.ToInt32(response.Split("-")[1]);
                switch(option){
                    case 1:{
                        await SendMessage(Constants.ACK, client);
                        // SOLO GAME
                        notvalid = false;
                    }
                    break;
                    case 2:{
                        // VERSUS GAME
                        _logger.LogInformation("Client acquire a vs mode, asking for room id");
                        
                        if(response.Contains(Constants.ROOM)){
                            string num = response.Split("-")[1];
                            int roomId  = -1, limit = 2;
                            if(num.Contains(",")){
                                roomId = Convert.ToInt32(num.Split(",")[0]);
                                limit = Convert.ToInt32(num.Split(",")[1]);
                            }else{
                                roomId = Convert.ToInt32(num);
                            }
                            limit = limit < 2 ? 2 : limit;
                            _logger.LogInformation($"Client acquire room with: {limit} spaces (Default 2)");
                            if(!_rooms.ContainsKey(roomId)){
                                _logger.LogInformation($"The room with Id: {roomId} do not exists, creating one...");
                                Thread room = new(()=>NewRoom(roomId));
                                _rooms.Add(roomId, room);
                                _rooms[roomId].Start();
                                List<Socket> clients = new(){
                                    client
                                };
                                _maxRoomClients.Add(roomId, new(limit, 1));
                                _roomClients.Add(roomId, clients);
                                response = await SendMessageAndWaitResponse(Constants.ACK, client);
                            }else{
                                if(_maxRoomClients[roomId].Item1 <= _maxRoomClients[roomId].Item2){
                                    _roomClients[roomId].Add(client);
                                    response = await SendMessageAndWaitResponse(Constants.ACK, client);
                                }else{
                                    response = await SendMessageAndWaitResponse(Constants.NACK, client);   
                                }
                            }
                        }
                        notvalid = false;
                    }
                    break;
                    case 3:{
                        await SendMessage(Constants.ACK, client);
                        response = Constants.DISCONNECT;
                        notvalid = false;
                    }
                    break;
                    default:{
                        await SendMessage(Constants.NACK, client);
                        notvalid = true;
                    }
                    break;
                }
                retries = 0;
            }else{
                retries ++;
            }
            if(retries > 10){
                _logger.LogCritical("Client disconnected by unsecure way");
                notvalid = false;
                response = Constants.DISCONNECT;
            }
        }while(notvalid);
        return response;
    }
    private async void NewRoom (Object roomId){
        int id = Convert.ToInt32(roomId);
        while(true){
            _logger.LogInformation($"RoomId: {id}");
            var connectedClients = _roomClients[id];
            foreach(var client in connectedClients){
                if(client.RemoteEndPoint is IPEndPoint remoteEndpoint){
                    _logger.LogInformation($"{remoteEndpoint.Address.ToString()}:{remoteEndpoint.Port}");
                }                
            }
            await Task.Delay(1000);
        }
    }
    private async Task StartConnection(Socket client){
        try{
            await _monitor.ChangeClientStatus(client, STATUS.CONNECTED);
            bool active = true;
            await _monitor.ChangeClientStatus(client, STATUS.IN_PROGRES);
            int emptyMessages = 0;
            while(active){
                var response = await GetMessage(client);
                await _monitor.ChangeClientStatus(client, STATUS.EXECUTING);
                if(response.Contains(Constants.CLIENT)){
                    await SendMessage(Constants.ACK, client);
                }else if(response.Contains(Constants.NEWSESSION)){
                    response = await NewSession(client);   
                }
                if(response.Contains(Constants.DISCONNECT)){
                    _logger.LogInformation("Client aquire disconection");
                    await _monitor.ChangeClientStatus(client, STATUS.DISCONNECTING);
                    await SendMessage(Constants.ACK, client);
                    active = false;
                    _logger.LogInformation("Client disconnected");
                }
                if(response == ""){
                    emptyMessages ++;
                }
                if(emptyMessages > 5){
                    _logger.LogError("Client do not respond, disconnecting");
                    await _monitor.ChangeClientStatus(client, STATUS.FAIL);
                    active = false;
                }
                Console.WriteLine($"Response-{response}");
            }
        }catch(Exception ex){
            await _monitor.ChangeClientStatus(client, STATUS.FAIL);
            _logger.LogWarning(ex.Message);
        }
    }
    private async Task SoloGame(Socket client){
        try{
            int difficult = 4;
            var buffer = new byte[2048];
            var recieved = await client.ReceiveAsync(buffer, SocketFlags.None);
            _logger.LogInformation("Reading buffer...");
            var response = Encoding.UTF8.GetString(buffer, 0, recieved);
            _logger.LogInformation("Client detected...");
            var bResponse = Encoding.UTF8.GetBytes(Constants.ACK);
            _ = await client.SendAsync(bResponse, SocketFlags.None);
            bool endGame = false;
            Dictionary<string, string> keyWords = new();
            int totalWords = 0, foundedWords = 0;
            _logger.LogInformation("Starting game...");
            string lastWord = "";
            int responsesFail = 0;
            while(!endGame){
                try{
                    if(responsesFail > 5){
                        _logger.LogError("Client disconnect unsecure...");
                        response = Constants.DISCONNECT;
                    }
                    if(response == Constants.DISCONNECT){
                        _logger.LogInformation("Client acquire disconnection...");
                        bResponse = Encoding.UTF8.GetBytes(Constants.ACK);
                        _ = await client.SendAsync(bResponse, SocketFlags.None);
                        _logger.LogInformation("Client disconnected");
                        endGame = true;
                        _logger.LogInformation("Ending game...");
                        _logger.LogInformation("Clearing data...");
                        buffer = new byte[2048];
                        recieved = 0;
                        response = "";
                        bResponse = new byte[2048];
                        keyWords = new();
                        _logger.LogInformation("Data cleared...");
                        endGame = true;
                    }else if(response != Constants.EMPTY){
                        responsesFail = 0;
                        _logger.LogInformation("Reading buffer...");
                        string?[] responseSplitted;
                        string code = "", content = "";
                        _logger.LogInformation("Getting code...");
                        if(response.Contains("-")){
                            responseSplitted = response.Split('-');
                            code = responseSplitted[0];    
                            content = responseSplitted[1];
                        }else{
                            code = response;
                        }
                        _logger.LogInformation(code);
                        if(code == Constants.DIFF){
                            _logger.LogInformation("Setting difficult...");
                            difficult = Convert.ToInt32(content);
                            _logger.LogInformation("Difficult set...");
                        }else if(code == Constants.CREATEMAP){
                            responsesFail = 0;
                            _logger.LogInformation("Setting map...");
                            Random rand = new();
                            int len = difficult / 2;
                            totalWords = len;
                            _logger.LogInformation("Getting words...");
                            for(int x = 0; x < len; x++){
                                int wordPosition = rand.Next(0, _words.Count());
                                string word = _words.ElementAt(wordPosition);
                                string dictKey = "";
                                for(int y = 0; y < len; y++){
                                    do{
                                        int j = rand.Next(0, len);
                                        int i = rand.Next(0, len);
                                        dictKey = $"{i}:{j}";
                                    }while(keyWords.ContainsKey(dictKey));
                                    keyWords.Add(dictKey, word);
                                    _logger.LogInformation(dictKey);
                                }
                            }
                            _logger.LogInformation("Map set...");
                            _logger.LogInformation("Sending response to client...");
                            bResponse = Encoding.UTF8.GetBytes(Constants.ACK);
                            _ = await client.SendAsync(bResponse, SocketFlags.None);
                            _logger.LogInformation("Response sent...");
                        }else if(code == Constants.COORD){
                            responsesFail = 0;
                            string message = "";
                            if(foundedWords == totalWords){
                                message = Constants.ENDGAME;
                            }else{
                                message = keyWords.ContainsKey(content) ? $"{Constants.ACK}-{keyWords[content]}": Constants.NACK;
                                if(lastWord == string.Empty && message != Constants.NACK) lastWord = keyWords[content];
                                else if(lastWord != string.Empty && message != Constants.NACK){
                                    message = lastWord != keyWords[content] ? Constants.CLEAR : message;
                                    foundedWords = message != Constants.CLEAR ? foundedWords + 1 : 0;
                                    lastWord = "";
                                }
                            }
                            bResponse = Encoding.UTF8.GetBytes(message);
                            _ = await client.SendAsync(bResponse, SocketFlags.None);
                            _logger.LogInformation("Response sent...");
                        }
                    }else{
                        responsesFail += 1;
                        _logger.LogInformation("Waiting for client response...");
                    }
                }catch(Exception ex){
                    _logger.LogCritical(ex.Message);
                    endGame = true;
                    bResponse = Encoding.UTF8.GetBytes(Constants.ERROR);
                    _ = await client.SendAsync(bResponse, SocketFlags.None);
                }finally{
                    if(!endGame){
                        buffer = new byte[2048];
                        recieved = await client.ReceiveAsync(buffer, SocketFlags.None);
                        _logger.LogInformation("Reading buffer...");
                        response = Encoding.UTF8.GetString(buffer, 0, recieved);
                    }
                }
            }
            if(!endGame) _logger.LogInformation("Sending response...");
        }catch(Exception ex){
            _logger.LogCritical(ex.Message);
        }
    }
}
