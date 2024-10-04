using CommandLine;
namespace SocketMemory.Models{
    public class Options{
        [Option('h', Required=false, HelpText ="Help menu", Default = true)]
        public bool Help {get; set;} = false;
        [Option('i', "interface", Required = false, HelpText = "Determines which interface do you want to use like (eth0 or wlan0) (Default = wlan0)")]
        public string Inter {get; set;} = "wlan0";
        [Option('m', "mode", Required = false, HelpText = "Determine which mode do you want to use (server or client) (Default = server)")]
        public string Mode {get; set;} = "server";
        [Option("ip", Required = false, HelpText = "Used only to specify the IP for the server (Only usable when is at client mode)")]
        public string Ip {get; set;} = "127.0.0.1";
        [Option("port", Required = false, HelpText = "Used only to specify the Port for the server (Only usable when is at client mode)(Default 9595)")]
        public int Port {get; set;} = 9595;
        [Option("difficult", Required = false, HelpText = "Used to specify the difficult of the game (4x4, 6x6, 8x8...) just type the size (4, 6, 8...)")]
        public int Difficult {get; set;} = 0;
    }
}
