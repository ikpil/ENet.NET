namespace ENet.NET
{
    public enum ENetSocketOption
    {
        ENET_SOCKOPT_NONBLOCK = 1,
        ENET_SOCKOPT_BROADCAST = 2,
        ENET_SOCKOPT_RCVBUF = 3,
        ENET_SOCKOPT_SNDBUF = 4,
        ENET_SOCKOPT_REUSEADDR = 5,
        ENET_SOCKOPT_RCVTIMEO = 6,
        ENET_SOCKOPT_SNDTIMEO = 7,
        ENET_SOCKOPT_ERROR = 8,
        ENET_SOCKOPT_NODELAY = 9,
        ENET_SOCKOPT_IPV6_V6ONLY = 10,
        ENET_SOCKOPT_TTL = 11,
    }
}