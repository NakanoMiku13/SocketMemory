using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using CommandLine;
using SocketMemory.Repositories;
using SocketMemory.Models;
namespace SocketMemory;
class Program
{
    private const string ACK = "|ACK|", CLIENT = "|CLIENT|", DIFF = "|DIFF|", ERROR = "|ERR|", EMPTY = "", DISCONNECT = "|END|",
    CREATEMAP = "|CREATE|", COORD = "|COORD|", NACK = "|NACK|", CLEAR = "|CLR|", ENDGAME = "|ENDGAME|";
    private static List<string> _words = new()
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
    async static Task Main(string[] args)
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = factory.CreateLogger("Program");
        string intFace = "wlan0";
        int port = 9595, difficult = 0;
        string serverIp = "";
        bool isServer = true, helpMenu = false;
        logger.LogInformation("Starting server...");
        if(args.Length <= 0){
            logger.LogWarning("No interface selected, using default (wlan0)");
            logger.LogWarning("No Port selected, using default (9595)");
            logger.LogWarning("No mode selected, setting default (SERVER MODE)");
        }else{
            Parser.Default.ParseArguments<Options>(args)
            .WithParsed(p => {
                helpMenu = p.Help;
                intFace = p.Inter;
                isServer = p.Mode == "server";
                port = p.Port;
                serverIp = p.Ip;
                difficult = p.Difficult;
            });
        }
        logger.LogInformation("Getting Base Ip");
        NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
        IPAddress? ip = null;
        foreach(var inface in networkInterfaces){
            try{
                var properties = inface.GetIPProperties();
                IPAddress? ipTemp = null;
                foreach(var ipData in properties.UnicastAddresses) if(ipData.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork){ ipTemp = ipData.Address; break;}
                if(intFace == inface.Name){
                    ip = ipTemp;
                    logger.LogInformation("Ip set successfully...");
                }
            }catch(Exception ex){
                logger.LogError(ex.Message);
            }
        }
        if(ip == null){
            logger.LogError("Not Ip found...");
            logger.LogInformation("Setting localhost as default ip...");
            var hostName = Dns.GetHostName();
            IPHostEntry localhost = await Dns.GetHostEntryAsync(hostName);
            ip = localhost.AddressList[0];
        }
        IPEndPoint endpoint = new(ip, port);
        if(isServer){
            logger.LogInformation("Starting server mode...");
            using Socket listener = new(
                endpoint.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp
            );
            logger.LogInformation("Binding ip and port...");
            listener.Bind(endpoint);
            logger.LogInformation("Setting max 100 clients");
            listener.Listen(100);
            logger.LogInformation($"Server started at: {ip}:{port}");
            while(true){
                logger.LogInformation("Waiting for new connection...");
                var handler = await listener.AcceptAsync();
                logger.LogInformation("Connection detected...");
                try{
                    var buffer = new byte[2048];
                    var recieved = await handler.ReceiveAsync(buffer, SocketFlags.None);
                    logger.LogInformation("Reading buffer...");
                    var response = Encoding.UTF8.GetString(buffer, 0, recieved);
                    logger.LogInformation("Client detected...");
                    var bResponse = Encoding.UTF8.GetBytes(ACK);
                    _ = await handler.SendAsync(bResponse, SocketFlags.None);
                    bool endGame = false;
                    Dictionary<string, string> keyWords = new();
                    int totalWords = 0, foundedWords = 0;
                    logger.LogInformation("Starting game...");
                    string lastWord = "";
                    int responsesFail = 0;
                    while(!endGame){
                        try{
                            if(responsesFail > 5){
                                logger.LogError("Client disconnect unsecure...");
                                response = DISCONNECT;
                            }
                            if(response == DISCONNECT){
                                logger.LogInformation("Client acquire disconnection...");
                                bResponse = Encoding.UTF8.GetBytes(ACK);
                                _ = await handler.SendAsync(bResponse, SocketFlags.None);
                                logger.LogInformation("Client disconnected");
                                endGame = true;
                                logger.LogInformation("Ending game...");
                                logger.LogInformation("Clearing data...");
                                buffer = new byte[2048];
                                recieved = 0;
                                response = "";
                                bResponse = new byte[2048];
                                keyWords = new();
                                logger.LogInformation("Data cleared...");
                                endGame = true;
                            }else if(response != EMPTY){
                                responsesFail = 0;
                                logger.LogInformation("Reading buffer...");
                                string?[] responseSplitted;
                                string code = "", content = "";
                                logger.LogInformation("Getting code...");
                                if(response.Contains("-")){
                                    responseSplitted = response.Split('-');
                                    code = responseSplitted[0];    
                                    content = responseSplitted[1];
                                }else{
                                    code = response;
                                }
                                logger.LogInformation(code);
                                if(code == DIFF){
                                    logger.LogInformation("Setting difficult...");
                                    difficult = Convert.ToInt32(content);
                                    logger.LogInformation("Difficult set...");
                                }else if(code == CREATEMAP){
                                    responsesFail = 0;
                                    logger.LogInformation("Setting map...");
                                    Random rand = new();
                                    int len = difficult / 2;
                                    totalWords = len;
                                    logger.LogInformation("Getting words...");
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
                                            logger.LogInformation(dictKey);
                                        }
                                    }
                                    logger.LogInformation("Map set...");
                                    logger.LogInformation("Sending response to client...");
                                    bResponse = Encoding.UTF8.GetBytes(ACK);
                                    _ = await handler.SendAsync(bResponse, SocketFlags.None);
                                    logger.LogInformation("Response sent...");
                                }else if(code == COORD){
                                    responsesFail = 0;
                                    string message = "";
                                    if(foundedWords == totalWords){
                                        message = ENDGAME;
                                    }else{
                                        message = keyWords.ContainsKey(content) ? $"{ACK}-{keyWords[content]}": NACK;
                                        if(lastWord == string.Empty && message != NACK) lastWord = keyWords[content];
                                        else if(lastWord != string.Empty && message != NACK){
                                            message = lastWord != keyWords[content] ? CLEAR : message;
                                            foundedWords = message != CLEAR ? foundedWords + 1 : 0;
                                            lastWord = "";
                                        }
                                    }
                                    bResponse = Encoding.UTF8.GetBytes(message);
                                    _ = await handler.SendAsync(bResponse, SocketFlags.None);
                                    logger.LogInformation("Response sent...");
                                }
                            }else{
                                responsesFail += 1;
                                logger.LogInformation("Waiting for client response...");
                            }
                        }catch(Exception ex){
                            logger.LogCritical(ex.Message);
                            endGame = true;
                            bResponse = Encoding.UTF8.GetBytes(ERROR);
                            _ = await handler.SendAsync(bResponse, SocketFlags.None);
                        }finally{
                            if(!endGame){
                                buffer = new byte[2048];
                                recieved = await handler.ReceiveAsync(buffer, SocketFlags.None);
                                logger.LogInformation("Reading buffer...");
                                response = Encoding.UTF8.GetString(buffer, 0, recieved);
                            }
                        }
                    }
                    if(!endGame) logger.LogInformation("Sending response...");
                }catch(Exception ex){
                    logger.LogCritical(ex.Message);
                }
            }
        }else{
            logger.LogInformation("Attempting to create client...");
            ClientSocket socket = new(serverIp, port);
            logger.LogInformation("Stablishing connection with server...");
            var connected = await socket.Connect();
            if(!connected){
                logger.LogCritical("Error stablishing connection to the server...");
                return;
            }
            try{
                logger.LogInformation("Connected to server successfully...");
                if(difficult == 0){
                    logger.LogWarning("Difficult not set, asking to the user for one...");
                    Console.WriteLine("Difficult of the game (4, 6, 8...): ");
                    do{
                        difficult = Convert.ToInt32(Console.ReadLine());
                        if(difficult <= 0 && difficult > 4) Console.WriteLine("Difficult not valid, please set up one...");
                        if(difficult % 2 != 0 ) Console.WriteLine("Difficult not pair, please, use a pair number...");
                    }while((difficult <= 0 || difficult % 2 != 0) && difficult > 4);
                }
                logger.LogInformation("Sending data...");
                var response = await socket.SendMessage($"|DIFF|-{difficult}");
                if(response == ACK){
                    bool endGame = false;
                    logger.LogInformation("Getting response...");
                    response = await socket.SendMessage(CREATEMAP);
                    logger.LogInformation(response);
                    Dictionary<string, string> foundWords = new();
                    if(response == ACK) // This means that the map has been created
                        while(!endGame){
                            PrintMap(difficult, foundWords);
                            Console.WriteLine("Type the coord of the word (eg. 1:1, 2:1, 0:3, etc.):");
                            string coord = "";
                            do{
                                coord = Console.ReadLine();
                                if(coord == string.Empty) Console.WriteLine("Type a coord...");
                            }while(coord == string.Empty);
                            string mm = $"|COORD|-{coord}";
                            response = await socket.SendMessage(mm);
                            if(response.Contains(ACK)){
                                var collectedWord = response.Split("-")[1];
                                if(foundWords.ContainsKey(coord)) logger.LogWarning("Coordinate already set...");
                                else foundWords.Add(coord, collectedWord);
                            }else if(response.Contains(CLEAR)){
                                foundWords = new();
                            }else if(response.Contains(NACK)){
                                logger.LogWarning("Coordinate out of range...");
                            }else if(response == ENDGAME){
                                endGame = true;
                                PrintMap(difficult, foundWords);
                                logger.LogInformation("Game ended, thanks for playing...");
                            }
                        }
                }
            }catch(Exception ex){
                logger.LogCritical(ex.Message);
            }
            finally{
                string response = await socket.SendMessage(DISCONNECT);
                if(response == ACK) logger.LogInformation("Closing connection...");
                socket.Dispose();
            }
        }
    }
    public async static Task<string> SendMessage(Socket client, string message){
        _ = await client.SendAsync(Encoding.UTF8.GetBytes(message), SocketFlags.None);
        var buffer = new byte[2048];
        var recieved = await client.ReceiveAsync(buffer, SocketFlags.None);
        var response = Encoding.UTF8.GetString(buffer, 0, recieved);
        return response.ToString();
    }
    public static void PrintMap(int difficult, Dictionary<string, string> words){
        for(int j = 0; j < difficult / 2; j++){
            Console.Write("+\t");
            for(int i = 0; i < difficult / 2; i++){
                Console.Write("----------\t+\t");
            }
            Console.Write("\n|");
            Console.Write("\n");
            Console.Write("|\t");
            for(int i = 0; i < (difficult / 2) ; i++){
                string key = $"{j}:{i}";
                if(words.ContainsKey(key)){
                    string word = words[key];
                    int wlen = 10 - word.Length;
                    string final = $"{word}";
                    if(wlen > 0) for(int k = 0; k < wlen; k++) final += " ";
                    Console.Write($"{final}\t|\t");
                } 
                else Console.Write("**SECRET**\t|\t");
            }
            Console.Write("\n|");
            Console.Write("\n");
        }
        Console.Write("+\t");
        for(int i = 0; i < difficult / 2; i++){
            Console.Write("----------\t+\t");
        }
        Console.Write("\n");
    }
}