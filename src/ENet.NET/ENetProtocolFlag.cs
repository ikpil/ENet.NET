namespace ENet.NET
{
    public static class ENetProtocolFlag
    {
        public const ushort ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE = (1 << 7);
        public const ushort ENET_PROTOCOL_COMMAND_FLAG_UNSEQUENCED = (1 << 6);
        
        public const ushort ENET_PROTOCOL_HEADER_FLAG_PEER_EXTRA = (1 << 13);
        public const ushort ENET_PROTOCOL_HEADER_FLAG_COMPRESSED = (1 << 14);
        public const ushort ENET_PROTOCOL_HEADER_FLAG_SENT_TIME = (1 << 15);
        public const ushort ENET_PROTOCOL_HEADER_FLAG_MASK = ENET_PROTOCOL_HEADER_FLAG_PEER_EXTRA | ENET_PROTOCOL_HEADER_FLAG_COMPRESSED | ENET_PROTOCOL_HEADER_FLAG_SENT_TIME;
        
        public const ushort ENET_PROTOCOL_HEADER_SESSION_MASK = (3 << 11);
        public const ushort ENET_PROTOCOL_HEADER_SESSION_SHIFT = 1;
    }
}