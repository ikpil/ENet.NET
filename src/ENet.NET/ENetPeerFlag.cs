namespace ENet.NET
{
    public static class ENetPeerFlag
    {
        public const ushort ENET_PEER_FLAG_NEEDS_DISPATCH = (1 << 0);
        public const ushort ENET_PEER_FLAG_CONTINUE_SENDING = (1 << 1);
    }
}