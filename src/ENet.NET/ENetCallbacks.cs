using static ENet.NET.ENetDelegates;

namespace ENet.NET
{
    public class ENetCallbacks
    {
        public IENetAllocator allocator;

        public PacketCreateDelegate packet_create;
        public PacketDestroyDelegate packet_destroy;
    }
}