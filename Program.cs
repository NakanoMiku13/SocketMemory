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
            ServerSocket listener = new(ip, port);
            logger.LogInformation($"Server started at: {ip}:{port}");
            while(true){
                logger.LogInformation("Waiting for new connection...");
                await listener.AcceptClientMainMenu();
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
            logger.LogInformation("Connected to server successfully...");
            try{
                Dictionary<string, string> words = new();
                string response = await socket.SendMessageAndWaitResponse(Constants.NEWSESSION);
                response = response.Split('-')[1];
                string options = response;
                string uuid = Guid.NewGuid().ToString();
                logger.LogInformation(options);
                do{
                    int option = Convert.ToInt32(Console.ReadLine());
                    response = await socket.SendMessageAndWaitResponse($"{Constants.OPTION}-{option}");
                    if(response.Contains(Constants.NACK)){
                        logger.LogError("Option not valid");
                        logger.LogInformation(options);
                        await Task.Delay(200);
                    }
                }while(!response.Contains(Constants.ACK));
                logger.LogInformation("Type the room id (if you want to create one, add ',' and max players, eg; 145631,5 - Where 145631 is the room id, and 5 are the max players)");
                string? roomId = Console.ReadLine();
                if(roomId == null){
                    logger.LogCritical("Please, type something...");
                    return;
                }
                response = await socket.SendMessageAndWaitResponse($"{Constants.ROOM}-{roomId}");
                string message = "";
                if(response.Contains(Constants.ACK)){
                    logger.LogInformation("Room created successfully");
                    do{
                        response = await socket.WaitMessage();
                        if(response.Contains(Constants.WAIT)){
                            message = response.Split("-")[1];
                            logger.LogInformation(message);
                        }else if(response.Contains(Constants.ERROR)){
                            throw new Exception(response);
                        }else{
                            logger.LogInformation(response);
                        }
                    }while(!response.Contains(Constants.START));
                    bool endGame = false;
                    string id = "";
                    do{
                        message = await socket.WaitMessage();
                        logger.LogInformation(message);
                    }while(!message.Contains(Constants.PLAYER));
                    string turn = message;
                    turn = turn.Split("-")[1];
                    response = await socket.WaitMessage();
                    roomId = roomId.Contains(",") ? roomId.Split(",")[0] : roomId;
                    if(response.Contains(Constants.PLAYER_ID)){
                        id = response.Split("-")[1];
                        while(!endGame){
                            if(response.Contains(Constants.ERROR)){
                                logger.LogInformation($"Something fail: {response.Split('-')[1]}");
                                break;
                            }else if(turn.Equals(id)){
                                logger.LogInformation($"Its your turn (You are: {id})");
                                do{
                                    message = Console.ReadLine();
                                }while(message == string.Empty);
                                if(message != null && message.Contains(':')){
                                    await socket.SendMessage($"{Constants.PLAYER_ID}-{roomId}-{id}-{Constants.COORD}_{message}");
                                }else{
                                    await socket.SendMessage($"{Constants.PLAYER_ID}-{roomId}-{id}-{message}");
                                }
                            }else{
                                logger.LogInformation(response.Split('-')[1]);
                            }
                            response = await socket.WaitMessage();
                            logger.LogInformation(response);
                            if(response.Contains(Constants.COORD)){
                                var unzipped = response.Split('-');
                                if(!words.ContainsKey(unzipped[1])){
                                    words.Add(unzipped[1], unzipped[2]);
                                }
                                PrintMap(8, words);
                            }
                            turn = response.Split("-")[1];
                        }
                    }
                }else{
                    logger.LogCritical("The room where you trying to access is full");
                }
            }catch(Exception ex){
                logger.LogCritical(ex.Message);
            }finally{
                await socket.SendMessage(Constants.DISCONNECT);
                logger.LogInformation("Closing connection...");
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