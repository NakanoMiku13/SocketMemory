using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
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
    private Dictionary<Socket, int> _clientAtRoom;
    private Dictionary<int, List<Socket>> _roomClients;
    private Dictionary<int, (int, int)> _maxRoomClients;
    private Dictionary<int, (string, string)> _lastRoomMessage;
    private readonly Monitor _monitor;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ServerSocket(string ip = "localhost", int port = 9595, int maxClients = 100)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = _loggerFactory.CreateLogger("Server");
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
        InitVar();
    }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ServerSocket(IPAddress ip, int port = 9595, int maxClients = 100)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = _loggerFactory.CreateLogger("Server");
        _port = port;
        _endpoint = new(ip, _port);
        _ip = ip.ToString();
        _socket = new(
            _endpoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp
        );
        _socket.Bind(_endpoint);
        _socket.Listen(maxClients);
        _monitor = new();
        InitVar();
    }
    private void InitVar(){
        _clients = new();
        _roomClients = new();
        _rooms = new();
        _clientAtRoom = new();
        _maxRoomClients = new();
        _lastRoomMessage = new();
        
    }
    private async Task<Socket?> AcceptClient(int timeout = 15){
        var client = _socket.AcceptAsync();
        var timeoutDelay = Task.Delay(timeout * 1000);
        if(await Task.WhenAny(client, timeoutDelay) == client){
            _logger.LogInformation("Incoming connection detected");
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
        var client = await _socket.AcceptAsync();
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
                        response = await SendMessageAndWaitResponse(Constants.ACK, client);
                        if(response.Contains(Constants.ROOM)){
                            string num = response.Split("-")[1];
                            int roomId  = -1, limit = 2;
                            if(num.Contains(",")){
                                roomId = Convert.ToInt32(num.Split(",")[0]);
                                limit = Convert.ToInt32(num.Split(",")[1]);
                            }else{
                                roomId = Convert.ToInt32(num);
                            }
                            if(!_rooms.ContainsKey(roomId)){
                                limit = limit < 2 ? 2 : limit;
                                _logger.LogInformation($"Client acquire room with: {limit} spaces (Default 2)");
                                _logger.LogInformation($"The room with Id: {roomId} do not exists, creating one...");
                                try{
                                    Thread room = new(()=>WaitRoom(roomId));
                                    _rooms.Add(roomId, room);
                                    _rooms[roomId].Start();
                                    _lastRoomMessage.Add(roomId, new());
                                    List<Socket> clients = new(){
                                        client
                                    };
                                    _maxRoomClients.Add(roomId, (limit, 1));
                                    _roomClients.Add(roomId, clients);
                                    _clientAtRoom.Add(client, roomId);
                                    response = await SendMessageAndWaitResponse(Constants.ACK, client);
                                }catch(Exception ex){
                                    _logger.LogError(ex.Message);
                                    await SendMessage(Constants.NACK, client);
                                    ClearRoom(roomId);         
                                }
                            }else{
                                (int roomLimit, int currentCount) = _maxRoomClients[roomId];
                                _logger.LogInformation($"Room {roomId} {currentCount}/{roomLimit}");
                                if(roomLimit > currentCount){
                                    _roomClients[roomId].Add(client);
                                    await SendMessage(Constants.ACK, client);
                                    _maxRoomClients[roomId] = (roomLimit, currentCount + 1);
                                    _clientAtRoom.Add(client, roomId);
                                    _logger.LogInformation($"New client join the room: {currentCount}/{roomLimit}");
                                }else{
                                    response = await SendMessageAndWaitResponse(Constants.NACK, client);
                                    _logger.LogWarning("Client acquire to join a full room");
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
        Console.WriteLine(response);
        return response;
    }
    private async void WaitRoom (Object roomId){
        await Task.Delay(500);
        try{
            int id = Convert.ToInt32(roomId);
            // Wait room
            bool isFull = false;
            var connectedClients = _roomClients[id];
            (int limit, int clientsConnected) = _maxRoomClients[id];
            while(!isFull){
                connectedClients = _roomClients[id];
                (limit, clientsConnected) = _maxRoomClients[id];
                _logger.LogInformation($"RoomId: {id} - Limit: {limit} - Current: {clientsConnected}");
                await SendMessageToClients($"{Constants.WAIT}-Waiting for fullfill the room; current clients: {clientsConnected}/{limit}", connectedClients);
                if(clientsConnected >= limit) isFull = true;
                if(clientsConnected == 0){
                    ClearRoom(id);
                    break;
                }
                await Task.Delay(1000);
            }
            if(clientsConnected == 0 && _maxRoomClients.ContainsKey(id) && _roomClients.ContainsKey(id) && _rooms.ContainsKey(id)){
                ClearRoom(id);
                return;
            }
            List<Socket> onlineClients = new();
            foreach(var client in connectedClients){
                if(_monitor.IsActive(client)){
                    await SendMessage($"{Constants.START}", client);
                    onlineClients.Add(client);
                }
            }
            await VSGame(id, onlineClients);
            if(_maxRoomClients.ContainsKey(id) && _roomClients.ContainsKey(id) && _rooms.ContainsKey(id)){
                ClearRoom(id);
            }
        }catch(Exception ex){
            _logger.LogCritical(ex.Message);
        }
    }
    private async Task SendMessageToClients(string message, List<Socket> clients){
        await Task.Delay(50);
        foreach(var client in clients)
            if(_monitor.IsActive(client))
                await SendMessage(message, client);
    }
    private async Task VSGame(int roomId, List<Socket> clients){
        try{
            bool endGame = false, currentPlayerFail = false;
            Dictionary<string, string> keyWords = new();
            int totalWords = 0, difficult = 8, foundedWords = 0;
            Dictionary<Socket, int> scores = new();
            Dictionary<string, string> playersAnswers = new();
            string currentPlayer = "", currentWord = "", lastWord = "";
            _logger.LogInformation("Setting map...");
            await SendMessageToClients("Setting map...", clients);
            Random rand = new();
            int len = difficult / 2;
            totalWords = len;
            _logger.LogInformation("Getting words...");
            await SendMessageToClients("Getting words...", clients);
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
                }
            }
            await SendMessageToClients("Map ready...", clients);
            await Task.Delay(250);
            int firstPlayerIndex = rand.Next(0, clients.Count - 1);
            var firstPlayer = clients.ElementAt(firstPlayerIndex);
            currentPlayer = GetPlayerId(firstPlayer);
            await SendMessageToClients($"{Constants.PLAYER}-{currentPlayer}", clients);
            await Task.Delay(250);
            foreach(var client in clients){
                if(_monitor.IsActive(client)){
                    string clientId = GetPlayerId(client);
                    await SendMessage($"{Constants.PLAYER_ID}-{clientId}", client);
                }
                    
            }
            string? message = "", currentIdRecieved = "";
            while(!endGame){
                try{
                    bool roomFull = VerifyClients(clients), mainDisconnect = false;
                    _logger.LogCritical($"Room full {roomFull}");
                    if(!roomFull) endGame = true;
                    else{
                        // GAME
                        _logger.LogInformation("Waiting for client message");
                        do{
                            currentIdRecieved = _lastRoomMessage[roomId].Item1?.ToString();
                            message = _lastRoomMessage[roomId].Item2?.ToString();
                            roomFull = VerifyClients(clients);
                            mainDisconnect = ClientDisconnectAbrupt(clients, currentPlayer);
                        }while((message == "" || message == null || message == string.Empty) && !endGame && roomFull && !mainDisconnect);
                        if(!roomFull) {
                            await SendMessageToClients($"{Constants.ERROR}-The room do not have the minimum amount of players to continue", clients);
                            break;
                        }
                        if(mainDisconnect){
                            clients.RemoveAt(firstPlayerIndex);
                            await SendMessageToClients($"{Constants.ACK}-The player with turn, disconnect, choosing other player", clients);
                        }
                        if(message != string.Empty && message != null){
                            _logger.LogInformation("Player send coordinates");
                            if(message.Contains(Constants.COORD)){
                                string coord = message.Split('_')[1];
                                if(keyWords.ContainsKey(coord)){
                                    _logger.LogInformation("Player found a word");
                                    currentWord = keyWords[coord];
                                    if(lastWord == string.Empty){
                                        lastWord = currentWord;
                                        await SendMessageToClients($"{Constants.ACK}-Player found a word, can continue", clients);
                                        await SendMessageToClients($"{Constants.PLAYER}-{currentPlayer}", clients);
                                    }else{
                                        if(lastWord.Equals(currentWord)){
                                            // PLAYER GET POINTS
                                            playersAnswers.Add(currentWord, currentPlayer);
                                            await SendMessageToClients($"{Constants.ACK}-Player found a pair, gets one point, can continue", clients);
                                            await SendMessageToClients($"{Constants.PLAYER}-{currentPlayer}", clients);
                                            foundedWords ++;
                                            lastWord = "";
                                            currentWord = "";
                                        }else{
                                            lastWord = "";
                                            currentWord = "";
                                            // PLAYER FAIL
                                            await SendMessageToClients($"{Constants.ACK}-Player fail choosing, next turn", clients);
                                            int nextPlayerId = GetNextPlayer(firstPlayerIndex, clients.Count);
                                            currentPlayer = GetPlayerId(clients.ElementAt(nextPlayerId));
                                            firstPlayerIndex = nextPlayerId;
                                            await SendMessageToClients($"{Constants.PLAYER}-{currentPlayer}", clients);
                                        }
                                    }
                                }else{
                                    lastWord = "";
                                    currentWord = "";
                                    // PLAYER FAIL
                                    await SendMessageToClients($"{Constants.ACK}-Player fail choosing, next turn", clients);
                                    int nextPlayerId = GetNextPlayer(firstPlayerIndex, clients.Count);
                                    currentPlayer = GetPlayerId(clients.ElementAt(nextPlayerId));
                                    firstPlayerIndex = nextPlayerId;
                                    await Task.Delay(50);
                                    await SendMessageToClients($"{Constants.PLAYER}-{currentPlayer}", clients);
                                }
                            }else{
                                // WRONG MESSAGE FORMAT
                                await SendMessageToClients($"{Constants.ACK}-Player fail choosing, next turn", clients);
                                int nextPlayerId = GetNextPlayer(firstPlayerIndex, clients.Count);
                                currentPlayer = GetPlayerId(clients.ElementAt(nextPlayerId));
                                firstPlayerIndex = nextPlayerId;
                                await SendMessageToClients($"{Constants.PLAYER}-{currentPlayer}", clients);
                            }
                        }
                        if(foundedWords >= totalWords) {
                            // NEEDS TO ADD THE PLAYER WHO WON AND SCORES
                            await SendMessageToClients($"{Constants.ACK}-All words found, ending game", clients);
                            endGame = true;
                            break;
                        }
                        _logger.LogInformation($"User: {currentIdRecieved} send; {message}");
                        _lastRoomMessage[roomId] = new();
                    }
                }catch(Exception ex){
                    _logger.LogError(ex.Message);
                }
            }
        }catch(Exception ex){   
            _logger.LogCritical(ex.Message); 
            await SendMessageToClients($"{Constants.ERROR}-Something fail while creating the game...", clients);
        }finally{
            _logger.LogInformation($"Clearing data from room {roomId}");
            ClearRoom(roomId);
        }
    }
    private bool ClientDisconnectAbrupt(List<Socket> clients, string mainClient){
        foreach(var client in clients){
            var clientId = GetPlayerId(client);
            if(clientId.Equals(mainClient) && !_monitor.IsActive(client)) return true;
        }
        return false;
    }
    private int GetNextPlayer(int currentIndex, int clientLimit){
        return currentIndex >= (clientLimit - 1) ? 0 : currentIndex + 1;
    }
    private bool VerifyClients(List<Socket> clients){
        int onlineClients = 0;
        foreach(var client in clients) if(_monitor.IsActive(client)) onlineClients ++;
        return onlineClients > 1;
    }
    private string GetPlayerId(Socket client){
        string id = "";
        try{
            var remoteEndPoint = client.RemoteEndPoint as IPEndPoint;
            if(remoteEndPoint != null)
                id = $"{remoteEndPoint.Address}:{remoteEndPoint.Port}";
        }catch(Exception ex){
            _logger.LogWarning($"Error while getting a client id: {ex.Message}");
        }
        return id;
    }
    private void ClearRoom(int id){
        try{
            _logger.LogWarning("No clients into room, cleaning room");
            if(_rooms[id].IsAlive)
                _rooms[id]?.Join();
            var clients = _roomClients[id];
            _rooms.Remove(id);
            _lastRoomMessage.Remove(id);
            _maxRoomClients.Remove(id);
            foreach(var client in clients){
                _clientAtRoom.Remove(client);
            }
            _roomClients.Remove(id);
            _logger.LogInformation("Room removed");
        }catch(Exception ex){
            _logger.LogWarning(ex.Message);
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
                Console.WriteLine($"Response-{response}");
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
                    await _monitor.ChangeClientStatus(client, STATUS.DISCONECTED);
                    RemoveClientFromCount(client);
                }
                // Asuming there is a message pending for a thread (room)
                if(response.Contains(Constants.PLAYER_ID)){
                    var items = response.Split('-');
                    int roomId = Convert.ToInt32(items[1]);
                    string playerId = items[2], message = items[3];  
                    _lastRoomMessage[roomId] = (playerId, message);
                }
                if(response == ""){
                    emptyMessages ++;
                }
                if(emptyMessages > 5){
                    _logger.LogError("Client do not respond, disconnecting");
                    await _monitor.ChangeClientStatus(client, STATUS.FAIL);
                    RemoveClientFromCount(client);
                    active = false;
                }
                response = "";
            }
        }catch(Exception ex){
            await _monitor.ChangeClientStatus(client, STATUS.FAIL);
            RemoveClientFromCount(client);
            _logger.LogWarning(ex.Message);
        }
    }
    private void RemoveClientFromCount(Socket client){
        if(_clientAtRoom.ContainsKey(client)){
            int roomId = _clientAtRoom[client];
            (int limit, int count) = _maxRoomClients[roomId];
            _maxRoomClients[roomId] = (limit, count - 1);
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
