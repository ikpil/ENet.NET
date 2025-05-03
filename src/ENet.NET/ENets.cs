namespace ENet.NET;

public static class ENets
{
    public const int ENET_BUFFER_MAXIMUM = (1 + 2 * ENET_PROTOCOL_MAXIMUM_PACKET_COMMANDS);
    
    // protocols
    public const uint ENET_PROTOCOL_MINIMUM_MTU = 576;
    public const uint ENET_PROTOCOL_MAXIMUM_MTU = 4096;
    public const int ENET_PROTOCOL_MAXIMUM_PACKET_COMMANDS = 32;
    public const uint ENET_PROTOCOL_MINIMUM_WINDOW_SIZE = 4096;
    public const uint ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE = 65536;
    public const int ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT = 1;
    public const int ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT = 255;
    public const ushort ENET_PROTOCOL_MAXIMUM_PEER_ID = 0xFFFF;
    public const uint ENET_PROTOCOL_MAXIMUM_FRAGMENT_COUNT = 1024 * 1024;
    
    // peers
    public const int ENET_PEER_DEFAULT_ROUND_TRIP_TIME = 500;
    public const int ENET_PEER_DEFAULT_PACKET_THROTTLE = 32;
    public const int ENET_PEER_PACKET_THROTTLE_SCALE = 32;
    public const int ENET_PEER_PACKET_THROTTLE_COUNTER = 7;
    public const int ENET_PEER_PACKET_THROTTLE_ACCELERATION = 2;
    public const int ENET_PEER_PACKET_THROTTLE_DECELERATION = 2;
    public const int ENET_PEER_PACKET_THROTTLE_INTERVAL = 5000;
    public const int ENET_PEER_PACKET_LOSS_SCALE = (1 << 16);
    public const int ENET_PEER_PACKET_LOSS_INTERVAL = 10000;
    public const int ENET_PEER_WINDOW_SIZE_SCALE = 64 * 1024;
    public const int ENET_PEER_TIMEOUT_LIMIT = 32;
    public const int ENET_PEER_TIMEOUT_MINIMUM = 5000;
    public const int ENET_PEER_TIMEOUT_MAXIMUM = 30000;
    public const int ENET_PEER_PING_INTERVAL = 500;
    public const int ENET_PEER_UNSEQUENCED_WINDOWS = 64;
    public const int ENET_PEER_UNSEQUENCED_WINDOW_SIZE = 1024;
    public const int ENET_PEER_FREE_UNSEQUENCED_WINDOWS = 32;
    public const int ENET_PEER_RELIABLE_WINDOWS = 16;
    public const ushort ENET_PEER_RELIABLE_WINDOW_SIZE = 0x1000;
    public const int ENET_PEER_FREE_RELIABLE_WINDOWS = 8;
    
    // hosts
    public const int ENET_HOST_RECEIVE_BUFFER_SIZE = 256 * 1024;
    public const int ENET_HOST_SEND_BUFFER_SIZE = 256 * 1024;
    public const int ENET_HOST_BANDWIDTH_THROTTLE_INTERVAL = 1000;
    public const int ENET_HOST_DEFAULT_MTU = 1392;
    public const int ENET_HOST_DEFAULT_MAXIMUM_PACKET_SIZE = 32 * 1024 * 1024;
    public const int ENET_HOST_DEFAULT_MAXIMUM_WAITING_DATA = 32 * 1024 * 1024;
}