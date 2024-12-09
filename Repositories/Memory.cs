using System;
namespace SocketMemory.Repositories;
public class Memory{
    private int _size;
    private List<string> _words;
    public Memory(int size = 4)
    {
        _size = size;
        _words = new();
    }
    
}