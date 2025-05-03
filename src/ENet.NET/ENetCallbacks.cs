using System;


namespace ENet.NET;

public class ENetCallbacks
{
    public Func<object, ulong> ma;

    public delegate object MallocDelegate(long size);
    public delegate void FreeDelegate(object size);
    public delegate void NoMemoryDelegate();
    public delegate ENetPacket PacketCreateDelegate(object data, ulong dataLength, uint flags);
    public delegate void PacketDestroyDelegate(ENetPacket packet);
    
    public MallocDelegate malloc;
    public FreeDelegate free;
    public NoMemoryDelegate no_memory;

    public PacketCreateDelegate packet_create;
    public PacketDestroyDelegate packet_destroy;
}