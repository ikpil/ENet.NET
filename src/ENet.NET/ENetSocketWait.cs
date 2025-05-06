namespace ENet.NET
{
    public static class ENetSocketWait
    {
        public const uint ENET_SOCKET_WAIT_NONE = 0;
        public const uint ENET_SOCKET_WAIT_SEND = (1 << 0);
        public const uint ENET_SOCKET_WAIT_RECEIVE = (1 << 1);
        public const uint ENET_SOCKET_WAIT_INTERRUPT = (1 << 2);
    }
}