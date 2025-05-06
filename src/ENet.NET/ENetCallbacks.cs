using System;


namespace ENet.NET
{
    public class ENetCallbacks
    {
        public MallocDelegate malloc;
        public FreeDelegate free;
        public NoMemoryDelegate no_memory;

        public PacketCreateDelegate packet_create;
        public PacketDestroyDelegate packet_destroy;
    }
}