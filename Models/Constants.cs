namespace SocketMemory.Models;
public static class Constants{
    public static string ACK = "|ACK|", CLIENT = "|CLIENT|", DIFF = "|DIFF|", ERROR = "|ERR|", EMPTY = "", DISCONNECT = "|END|",
    CREATEMAP = "|CREATE|", COORD = "|COORD|", NACK = "|NACK|", CLEAR = "|CLR|", ENDGAME = "|ENDGAME|",
    MENU = "|MENU|", OPTIONS = "1) Solo game\n2) VS game\n3) Exit", NEWSESSION = "|MAP|",
    OPTION = "|OPTION|", ROOM = "|ROOM|", START = "|START|", WAIT = "|WAIT|";
}
public enum STATUS{
    CONNECTING,
    CONNECTED,
    IN_PROGRES,
    EXECUTING,
    FAIL,
    STOPPED,
    DISCONNECTING,
    DISCONECTED
}