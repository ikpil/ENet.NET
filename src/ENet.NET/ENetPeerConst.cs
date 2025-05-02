namespace ENet.NET;

public class ENetPeerConst
{
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
    public const int ENET_PEER_RELIABLE_WINDOW_SIZE = 0x1000;
    public const int ENET_PEER_FREE_RELIABLE_WINDOWS = 8;
}