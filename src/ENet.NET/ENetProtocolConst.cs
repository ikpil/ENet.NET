namespace ENet.NET;

public static class ENetProtocolConst
{
    public const uint ENET_PROTOCOL_MINIMUM_MTU = 576;
    public const uint ENET_PROTOCOL_MAXIMUM_MTU = 4096;
    public const uint ENET_PROTOCOL_MAXIMUM_PACKET_COMMANDS = 32;
    public const uint ENET_PROTOCOL_MINIMUM_WINDOW_SIZE = 4096;
    public const uint ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE = 65536;
    public const uint ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT = 1;
    public const uint ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT = 255;
    public const ushort ENET_PROTOCOL_MAXIMUM_PEER_ID = 0xFFFF;
    public const uint ENET_PROTOCOL_MAXIMUM_FRAGMENT_COUNT = 1024 * 1024;
}